using Application;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace UnitTests;

public class NotificationProcessingServiceTests
{
    private readonly BackgroundQueue<Notification> _backgroundQueueMock;
    private readonly INotificationService _notificationServiceMock;
    private readonly ILogger<NotificationProcessingService> _loggerMock;
    private readonly NotificationProcessingService _notificationProcessingService;

    public NotificationProcessingServiceTests()
    {
        _backgroundQueueMock = new BackgroundQueue<Notification>();
        _notificationServiceMock = Substitute.For<INotificationService>();
        _loggerMock = Substitute.For<ILogger<NotificationProcessingService>>();
        _notificationProcessingService = new NotificationProcessingService(
            _backgroundQueueMock, _notificationServiceMock, _loggerMock);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesNotifications()
    {
        // Arrange
        var stoppingToken = new CancellationTokenSource();
        var notification = new Notification { Type = "status", UserId = "user1", Message = "Hello" };
        _backgroundQueueMock.Queue(notification);

        // Act
        var executeTask = _notificationProcessingService.StartAsync(stoppingToken.Token);

        // Allow some time for the processing to occur
        await Task.Delay(100);
        stoppingToken.Cancel();
        await executeTask;

        // Assert
        await _notificationServiceMock.Received(1)
            .SendAsync(notification.Type, notification.UserId, notification.Message);
        _loggerMock.Received()
            .LogInformation(
                $"Notification sent: Type={notification.Type}, UserId={notification.UserId}, Message={notification.Message}");
    }

    [Fact]
    public async Task ExecuteAsync_LogsError_WhenExceptionThrown()
    {
        // Arrange
        var stoppingToken = new CancellationTokenSource();
        var notification = new Notification { Type = "status", UserId = "user1", Message = "Hello" };

        _backgroundQueueMock.Queue(notification);
        _notificationServiceMock.SendAsync(notification.Type, notification.UserId, notification.Message)
            .Throws(new Exception("Test exception"));

        // Act
        var executeTask = _notificationProcessingService.StartAsync(stoppingToken.Token);

        // Allow some time for the processing to occur
        await Task.Delay(100);
        stoppingToken.Cancel();
        await executeTask;

        // Assert
        _loggerMock.Received().LogError(Arg.Any<Exception>(), "Error processing notification: Test exception");
    }

    [Fact]
    public async Task ExecuteAsync_StopsGracefully_WhenCancelled()
    {
        // Arrange
        var stoppingToken = new CancellationTokenSource();

        // Act
        var executeTask = _notificationProcessingService.StartAsync(stoppingToken.Token);

        stoppingToken.Cancel();
        await executeTask;

        // Assert
        _loggerMock.Received().LogInformation("Operation canceled.");
    }
}