using Microsoft.Extensions.Options;

namespace Application;

using Polly;
using Polly.RateLimit;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class NotificationServiceImpl : INotificationService, IDisposable
{
    private readonly Gateway _gateway;
    private readonly Dictionary<string, AsyncRateLimitPolicy> _rateLimitPolicies;
    private readonly BlockingCollection<Notification> _queue;

    public NotificationServiceImpl(Gateway gateway, IOptions<RateLimitOptions> options)
    {
        _gateway = gateway;
        _rateLimitPolicies = new Dictionary<string, AsyncRateLimitPolicy>();

        foreach (var policy in options.Value.Policies)
        {
            _rateLimitPolicies[policy.Key] = Policy.RateLimitAsync(policy.Value.Limit, policy.Value.Period);
        }

        _queue = new BlockingCollection<Notification>();
    }

    public async void SendAsync(string type, string userId, string message)
    {
        if (!_rateLimitPolicies.ContainsKey(type))
        {
            throw new ArgumentException($"Unknown notification type: {type}");
        }

        var policy = _rateLimitPolicies[type];

        try
        {
            await policy.ExecuteAsync(async () =>
            {
                _gateway.Send(userId, message);
                await Task.CompletedTask;
            });
        }
        catch (RateLimitRejectedException)
        {
            Console.WriteLine($"Rate limit exceeded for {type} notifications to {userId}. Queuing message.");
            _queue.Add(new Notification { Type = type, UserId = userId, Message = message });
        }
    }

    public async Task ProcessQueue()
    {
        foreach (var notification in _queue.GetConsumingEnumerable())
        {
            try
            {
                var policy = _rateLimitPolicies[notification.Type];

                await policy.ExecuteAsync(async () =>
                {
                    _gateway.Send(notification.UserId, notification.Message);
                    await Task.CompletedTask;
                });
            }
            catch (RateLimitRejectedException)
            {
                Console.WriteLine(
                    $"Rate limit still exceeded for {notification.Type} notifications to {notification.UserId}. Re-queuing message.");
                _queue.Add(notification);
            }
        }

        await Task.Delay(TimeSpan.FromMinutes(1));
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _queue.Dispose();
    }
}