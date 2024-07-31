namespace Application;

public interface INotificationService
{
    Task SendAsync(string type, string userId, string message);
}