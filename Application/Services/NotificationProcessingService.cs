using Microsoft.Extensions.Logging;

namespace Application;

using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class NotificationProcessingService : BackgroundService
{
    private readonly BackgroundQueue<Notification> _backgroundQueue;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationProcessingService> _logger;

    public NotificationProcessingService(
        BackgroundQueue<Notification> backgroundQueue,
        INotificationService notificationService,
        ILogger<NotificationProcessingService> logger)
    {
        _backgroundQueue = backgroundQueue;
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Loop runs until the application is stopping
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (int index in Enumerable.Range(1, _backgroundQueue.GetCount()))
                {
                    var notification = _backgroundQueue.Dequeue();
                    await _notificationService.SendAsync(notification.Type, notification.UserId, notification.Message);
                    _logger.LogInformation($"Notification sent: Type={notification.Type}, UserId={notification.UserId}, Message={notification.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing notification: {ex.Message}");
            }
        }
    }
}