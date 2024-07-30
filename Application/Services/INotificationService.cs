namespace Application;

public interface INotificationService
{
    void Send(string type, string userId, string message);
}