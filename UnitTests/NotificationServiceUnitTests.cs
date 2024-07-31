using Application;
using Application.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UnitTests;

public class NotificationServiceTests
{
    private readonly Mock<Gateway> _gatewayMock;
    private readonly NotificationServiceImpl _service;

    public NotificationServiceTests()
    {
        _gatewayMock = new Mock<Gateway>();

        var rateLimitOptions = Options.Create(new RateLimitOptions
        {
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { "status", new RateLimitPolicy { Limit = 2, Period = TimeSpan.FromMinutes(1) } },
                { "news", new RateLimitPolicy { Limit = 1, Period = TimeSpan.FromDays(1) } },
                { "marketing", new RateLimitPolicy { Limit = 3, Period = TimeSpan.FromHours(1) } }
            }
        });

        _service = new NotificationServiceImpl(_gatewayMock.Object, rateLimitOptions,new BackgroundQueue<Notification>());
    }

    [Fact]
    public async Task Send_WithValidRateLimit_ShouldSendMessage()
    {
        // Arrange
        var type = "news";
        var userId = "user1";
        var message = "This is a news message.";

        // Act
        await _service.SendAsync(type, userId, message);
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
        await _service.SendAsync(type, userId, message);

        // Act
        // Send second message (should be queued due to rate limit)
        await _service.SendAsync(type, userId, message);
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
        await _service.SendAsync(type, userId, message);

        // Act
        // Send second message (should be queued due to rate limit)
        await _service.SendAsync(type, userId, message);
        await Task.Delay(TimeSpan.FromDays(1) + TimeSpan.FromMinutes(1)); // Wait for rate limit to reset

        // Assert
        _gatewayMock.Verify(g => g.Send(userId, message), Times.Exactly(2));
    }
}
