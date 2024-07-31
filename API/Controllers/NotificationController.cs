using Application;

namespace API.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost]
    public async Task<IActionResult> SendNotification(string type, string userId, string message)
    {
        await _notificationService.SendAsync(type, userId, message);
        return Ok("Notification sent or queued.");
    }
}
