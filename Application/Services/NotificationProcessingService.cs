namespace Application;

using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class NotificationProcessingService(
    INotificationService notificationService,
    BackgroundQueue<Notification> backgroundQueue)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Loop runs until the application is stopping
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (int index in Enumerable.Range(1, backgroundQueue.GetCount()))
                {
                    var notification = backgroundQueue.Dequeue();
                    await notificationService.SendAsync(notification.Type, notification.UserId, notification.Message);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
            catch (OperationCanceledException)
            {
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