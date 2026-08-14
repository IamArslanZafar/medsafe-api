using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

// [Authorize] (no role restriction) — Nurse, Physician and Admin all receive
// their own notifications, scoped server-side to the logged-in user.
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(new { unreadCount = count });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _notificationService.GetMyNotificationsAsync(unreadOnly, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkReadAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var count = await _notificationService.MarkAllReadAsync(cancellationToken);
        return Ok(new { updatedCount = count });
    }
}
