namespace Application;

public interface IGateway
{
    void Send(string userId, string message);
}

public class Gateway : IGateway
{
    public virtual void Send(string userId, string message)
    {
        Console.WriteLine($"sending message to user {userId}: {message}");
    }
}