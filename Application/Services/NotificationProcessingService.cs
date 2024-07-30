using Polly.RateLimit;

namespace Application;

using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class NotificationProcessingService : BackgroundService
{
    private readonly BlockingCollection<Notification> _queue;
    private readonly INotificationService _notificationService;

    public NotificationProcessingService(BlockingCollection<Notification> queue, INotificationService notificationService)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Loop runs until the application is stopping
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var notification in _queue.GetConsumingEnumerable())
            {
                try
                {
                    _notificationService.SendAsync(notification.Type, notification.UserId, notification.Message);
                }
                catch (OperationCanceledException)
                {
                    // Handle the cancellation
                    break;
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"Error processing notification: {ex.Message}");
                }
            }
        }
    }
}
