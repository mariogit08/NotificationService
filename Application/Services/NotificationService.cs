using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Polly;
using Polly.RateLimit;

namespace Application.Services;

public class NotificationServiceImpl : INotificationService
{
    private readonly Gateway _gateway;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AsyncRateLimitPolicy>> _rateLimitPolicies;
    private readonly BackgroundQueue<Notification> _backgroundQueue;
    private readonly RateLimitOptions _rateLimitOptions;

    public NotificationServiceImpl(Gateway gateway, IOptions<RateLimitOptions> options, BackgroundQueue<Notification> backgroundQueue)
    {
        _gateway = gateway;
        _backgroundQueue = backgroundQueue;
        _rateLimitOptions = options.Value;
        _rateLimitPolicies = new ConcurrentDictionary<string, ConcurrentDictionary<string, AsyncRateLimitPolicy>>();

        foreach (var policy in _rateLimitOptions.Policies)
        {
            _rateLimitPolicies[policy.Key] = new ConcurrentDictionary<string, AsyncRateLimitPolicy>();
        }
    }

    public async Task SendAsync(string type, string userId, string message)
    {
        if (!_rateLimitPolicies.ContainsKey(type))
        {
            throw new ArgumentException($"Unknown notification type: {type}");
        }

        var userPolicies = _rateLimitPolicies[type];

        if (!userPolicies.ContainsKey(userId))
        {
            var rateLimitPolicy = _rateLimitOptions.Policies[type];
            userPolicies[userId] = Policy.RateLimitAsync(rateLimitPolicy.Limit, rateLimitPolicy.Period);
        }

        var rateLimit = userPolicies[userId];

        try
        {
            await rateLimit.ExecuteAsync(async () =>
            {
                _gateway.Send(userId, message);
                await Task.CompletedTask;
            });
        }
        catch (RateLimitRejectedException)
        {
            Console.WriteLine($"Rate limit exceeded for {type} notifications to {userId}. Queuing message.");
            _backgroundQueue.Queue(new Notification { Type = type, UserId = userId, Message = message });
        }
    }
}