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
    public async Task<IActionResult> SendNotification([FromBody] NotificationInputModel notificationInputModel)
    {
        await _notificationService.SendAsync
            (notificationInputModel.Type, 
             notificationInputModel.UserId, 
             notificationInputModel.Message);
        
        return Ok("Notification sent or queued.");
    }

    public struct NotificationInputModel
    {
        public string Type { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
    }
}
