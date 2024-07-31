namespace UnitTests;

public class NotificationServiceImpl
{
    public class NotificationServiceImplTests
    {
        private readonly Mock<Gateway> _gatewayMock;
        private readonly Mock<IOptions<RateLimitOptions>> _optionsMock;
        private readonly Mock<BackgroundQueue<Notification>> _backgroundQueueMock;
        private readonly NotificationServiceImpl _notificationService;
        private readonly RateLimitOptions _rateLimitOptions;

        public NotificationServiceImplTests()
        {
            _gatewayMock = new Mock<Gateway>();
            _backgroundQueueMock = new Mock<BackgroundQueue<Notification>>();

            _rateLimitOptions = new RateLimitOptions
            {
                Policies = new ConcurrentDictionary<string, RateLimitPolicy>
                {
                    ["email"] = new RateLimitPolicy { Limit = 1, Period = TimeSpan.FromMinutes(1) },
                    ["sms"] = new RateLimitPolicy { Limit = 2, Period = TimeSpan.FromMinutes(1) }
                }
            };
            _optionsMock = new Mock<IOptions<RateLimitOptions>>();
            _optionsMock.Setup(o => o.Value).Returns(_rateLimitOptions);

            _notificationService = new NotificationServiceImpl(_gatewayMock.Object, _optionsMock.Object, _backgroundQueueMock.Object);
        }

        [Fact]
        public async Task SendAsync_ThrowsArgumentException_WhenUnknownType()
        {
            // Arrange
            string unknownType = "push";
            string userId = "user1";
            string message = "Hello";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _notificationService.SendAsync(unknownType, userId, message));
        }

        [Fact]
        public async Task SendAsync_SendsMessage_WhenNotRateLimited()
        {
            // Arrange
            string type = "email";
            string userId = "user1";
            string message = "Hello";

            // Act
            await _notificationService.SendAsync(type, userId, message);

            // Assert
            _gatewayMock.Verify(g => g.Send(userId, message), Times.Once);
            _backgroundQueueMock.Verify(bq => bq.Queue(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task SendAsync_QueuesMessage_WhenRateLimited()
        {
            // Arrange
            string type = "email";
            string userId = "user1";
            string message = "Hello";

            // First message to hit the rate limit
            await _notificationService.SendAsync(type, userId, message);

            // Act
            await _notificationService.SendAsync(type, userId, message);

            // Assert
            _gatewayMock.Verify(g => g.Send(userId, message), Times.Once);
            _backgroundQueueMock.Verify(bq => bq.Queue(It.Is<Notification>(n => n.Type == type && n.UserId == userId && n.Message == message)), Times.Once);
        }
    }
}
}