namespace Application;

public interface INotificationService
{
    void SendAsync(string type, string userId, string message);
}