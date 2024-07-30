using System.Collections.Concurrent;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace UnitTests;

public class Tests
{
    [Fact]
    public void NotificationService_ShouldSendMessageWithout()
    {
        
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using Polly.RateLimit;
using Xunit;

public class NotificationServiceTests : IDisposable
{
    private readonly Mock<Gateway> _gatewayMock;
    private readonly NotificationServiceImpl _service;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public NotificationServiceTests()
    {
        _gatewayMock = new Mock<Gateway>();
        _cancellationTokenSource = new CancellationTokenSource();

        var rateLimitPolicies = new ConcurrentDictionary<string, AsyncRateLimitPolicy>
        {
            ["status"] = Policy.RateLimitAsync(2, TimeSpan.FromMinutes(1)),
            ["news"] = Policy.RateLimitAsync(1, TimeSpan.FromDays(1)),
            ["marketing"] = Policy.RateLimitAsync(3, TimeSpan.FromHours(1))
        };

        _service = new NotificationServiceImpl(_gatewayMock.Object, rateLimitPolicies, _cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        _service.Dispose();
        _cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task Send_WithValidRateLimit_ShouldSendMessage()
    {
        // Arrange
        var type = "news";
        var userId = "user1";
        var message = "This is a news message.";

        // Act
        _service.Send(type, userId, message);
        await Task.Delay(100); // Small delay to allow async execution

        // Assert
        _gatewayMock.Verify(g => g.Send(userId, message), Times.Once);
    }

    [Fact]
    public async Task Send_WithExceededRateLimit_ShouldQueueMessage()
    {
        // Arrange
        var type = "news";
        var userId = "user1";
        var message = "This is a news message.";

        // Send first message (should be sent immediately)
        _service.Send(type, userId, message);

        // Act
        // Send second message (should be queued due to rate limit)
        _service.Send(type, userId, message);
        await Task.Delay(100); // Small delay to allow async execution

        // Assert
        _gatewayMock.Verify(g => g.Send(userId, message), Times.Once);
    }

    [Fact]
    public async Task ProcessQueue_ShouldSendQueuedMessages()
    {
        // Arrange
        var type = "news";
        var userId = "user1";
        var message = "This is a news message.";

        // Send first message (should be sent immediately)
        _service.Send(type, userId, message);

        // Act
        // Send second message (should be queued due to rate limit)
        _service.Send(type, userId, message);
        await Task.Delay(TimeSpan.FromDays(1) + TimeSpan.FromMinutes(1)); // Wait for rate limit to reset

        // Assert
        _gatewayMock.Verify(g => g.Send(userId, message), Times.Exactly(2));
    }
}

// Update the NotificationServiceImpl class to accept rateLimitPolicies and cancellationToken in the constructor
class NotificationServiceImpl : INotificationService, IDisposable
{
    private readonly Gateway _gateway;
    private readonly ConcurrentDictionary<string, AsyncRateLimitPolicy> _rateLimitPolicies;
    private readonly BlockingCollection<Notification> _queue;
    private readonly CancellationToken _cancellationToken;
    private readonly Task _backgroundJob;

    public NotificationServiceImpl(Gateway gateway, ConcurrentDictionary<string, AsyncRateLimitPolicy> rateLimitPolicies, CancellationToken cancellationToken)
    {
        _gateway = gateway;
        _rateLimitPolicies = rateLimitPolicies;
        _queue = new BlockingCollection<Notification>();
        _cancellationToken = cancellationToken;

        _backgroundJob = Task.Run(ProcessQueue, _cancellationToken);
    }

    public async void Send(string type, string userId, string message)
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

    private async Task ProcessQueue()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            if (_queue.TryTake(out var notification, Timeout.Infinite, _cancellationToken))
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
                    Console.WriteLine($"Rate limit still exceeded for {notification.Type} notifications to {notification.UserId}. Re-queuing message.");
                    _queue.Add(notification);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), _cancellationToken);
        }
    }

    public void Dispose()
    {
        _queue.Dispose();
    }
}

class Gateway
{
    // already implemented
    public virtual void Send(string userId, string message)
    {
        Console.WriteLine($"sending message to user {userId}: {message}");
    }
}

class Notification
{
    public string Type { get; set; }
    public string UserId { get; set; }
    public string Message { get; set; }
}
