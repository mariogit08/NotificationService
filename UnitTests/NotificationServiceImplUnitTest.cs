using System.Collections.Concurrent;
using Application;
using Application.Services;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace UnitTests;

public class NotificationServiceImplTests
{
    [Fact]
    public void SendAsync_ThrowsArgumentException_WhenUnknownType()
    {
        // Arrange
        var (notificationService,_,_) = SetupNotificationServiceMocks();
        string unknownType = "push";
        string userId = "user1";
        string message = "Hello";

        // Act
        Action act = () => notificationService.SendAsync(unknownType, userId, message);

        //Assert
        act.Throws<Exception>();
    }

    [Fact]
    public async Task SendAsync_SendsMessage_WhenNotRateLimited()
    {
        // Arrange
        var (notificationService,backgroundQueueMock,gatewayMock) = SetupNotificationServiceMocks();
        string type = "status";
        string userId = "user1";
        string message = "Hello";

        // Act
        await notificationService.SendAsync(type, userId, message);

        // Assert
        gatewayMock.Received(1).Send(userId, message);
        backgroundQueueMock.DidNotReceive().Queue(Arg.Any<Notification>());
    }

    [Fact]
    public async Task SendAsync_QueuesMessage_WhenRateLimited()
    {
        // Arrange
        var (notificationService,backgroundQueueMock,gatewayMock) = SetupNotificationServiceMocks();
        string type = "news";
        string userId = "user1";
        string message = "Hello";

        // First message to hit the rate limit
        await notificationService.SendAsync(type, userId, message);

        // Act
        await notificationService.SendAsync(type, userId, message);

        // Assert
        gatewayMock.Received(1).Send(userId, message);
        backgroundQueueMock.Received(1)
            .Queue(Arg.Is<Notification>(n => n.Type == type && n.UserId == userId && n.Message == message));
    }

    [Fact]
    public async Task SendAsync_RespectsRateLimitForDifferentTypes()
    {
        // Arrange
        var (notificationService,backgroundQueueMock,gatewayMock) = SetupNotificationServiceMocks();
        string typeStatus = "status";
        string typeNews = "news";
        string typeMarketing = "marketing";
        string userId = "user1";
        string message = "Hello";

        // Act
        await notificationService.SendAsync(typeStatus, userId, message);
        await notificationService.SendAsync(typeStatus, userId, message); // 2 messages should be fine for "status"
        await notificationService.SendAsync(typeNews, userId, message); // 1 message should be fine for "news"
        await notificationService.SendAsync(typeMarketing, userId, message);
        await notificationService.SendAsync(typeMarketing, userId, message);
        await notificationService.SendAsync(typeMarketing, userId,
            message); // 3 messages should be fine for "marketing"

        // Assert
        gatewayMock.Received(6).Send(userId, message);
        backgroundQueueMock.DidNotReceive().Queue(Arg.Any<Notification>());
    }

    [Fact]
    public async Task SendAsync_QueuesMessageWhenExceedingRateLimitForDifferentTypes()
    {
        // Arrange
        var (notificationService,backgroundQueueMock,gatewayMock) = SetupNotificationServiceMocks();
        string typeStatus = "status";
        string typeNews = "news";
        string typeMarketing = "marketing";
        string userId = "user1";
        string message = "Hello";

        // Act
        await notificationService.SendAsync(typeStatus, userId, message);
        await notificationService.SendAsync(typeStatus, userId, message);
        await notificationService.SendAsync(typeStatus, userId, message); // 3rd message should be queued for "status"

        await notificationService.SendAsync(typeNews, userId, message);
        await notificationService.SendAsync(typeNews, userId, message); // 2nd message should be queued for "news"

        await notificationService.SendAsync(typeMarketing, userId, message);
        await notificationService.SendAsync(typeMarketing, userId, message);
        await notificationService.SendAsync(typeMarketing, userId, message);
        await notificationService.SendAsync(typeMarketing, userId,
            message); // 4th message should be queued for "marketing"

        // Assert
        gatewayMock.Received(6).Send(userId, message);
        backgroundQueueMock.Received(1)
            .Queue(Arg.Is<Notification>(n => n.Type == typeStatus && n.UserId == userId && n.Message == message));
        backgroundQueueMock.Received(1)
            .Queue(Arg.Is<Notification>(n => n.Type == typeNews && n.UserId == userId && n.Message == message));
        backgroundQueueMock.Received(1)
            .Queue(Arg.Is<Notification>(n => n.Type == typeMarketing && n.UserId == userId && n.Message == message));
    }

    (NotificationServiceImpl, BackgroundQueue<Notification>, IGateway) SetupNotificationServiceMocks()
    {
        var gateway = Substitute.For<IGateway>();
        var backgroundQueue = Substitute.For<BackgroundQueue<Notification>>();
        
        var rateLimitOptions = new RateLimitOptions
        {
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { "status", new RateLimitPolicy { Limit = 2, Period = TimeSpan.FromMinutes(1) } },
                { "news", new RateLimitPolicy { Limit = 1, Period = TimeSpan.FromDays(1) } },
                { "marketing", new RateLimitPolicy { Limit = 3, Period = TimeSpan.FromHours(1) } }
            }
        };
        
        var optionsMock = Substitute.For<IOptions<RateLimitOptions>>();
        optionsMock.Value.Returns(rateLimitOptions);
        
        var notificationService = new NotificationServiceImpl(
            gateway,
            optionsMock,
            backgroundQueue);
            
        return (notificationService, backgroundQueue, gateway);
    }
}