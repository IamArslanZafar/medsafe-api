using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

// Powers the in-app notification bell — always scoped to the logged-in user
// (from JWT via ICurrentUserService), never a caller-supplied userId.
public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        return await _db.IncidentNotifications
            .AsNoTracking()
            .CountAsync(x => x.RecipientUserId == userId && !x.IsRead, cancellationToken);
    }

    public async Task<MyNotificationsResponse> GetMyNotificationsAsync(bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var query = _db.IncidentNotifications
            .AsNoTracking()
            .Where(x => x.RecipientUserId == userId);

        if (unreadOnly)
            query = query.Where(x => !x.IsRead);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationListItemDto
            {
                Id = x.Id,
                IncidentReportId = x.IncidentReportId,
                IncidentReportNumber = x.IncidentReport.IncidentReportNumber,
                AlertRuleId = x.AlertRuleId,
                RuleId = x.AlertRule != null ? x.AlertRule.RuleId : null,
                RuleName = x.AlertRule != null ? x.AlertRule.Name : null,
                Title = x.Title ?? "Medication Safety Alert",
                Message = x.Message ?? string.Empty,
                UrgencyId = x.UrgencyId,
                UrgencyCode = x.Urgency != null ? x.Urgency.Code : null,
                UrgencyName = x.Urgency != null ? x.Urgency.Name : null,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt,
                CreatedAt = x.CreatedDate
            })
            .ToListAsync(cancellationToken);

        var unreadCount = await _db.IncidentNotifications
            .AsNoTracking()
            .CountAsync(x => x.RecipientUserId == userId && !x.IsRead, cancellationToken);

        return new MyNotificationsResponse
        {
            Items = items,
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // Scoped to the current user by design — a notification ID alone is not enough,
    // otherwise one user could mark another user's notification as read.
    public async Task<MarkNotificationReadResponse?> MarkReadAsync(int notificationId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var notification = await _db.IncidentNotifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.RecipientUserId == userId, cancellationToken);

        if (notification == null)
            return null;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new MarkNotificationReadResponse
        {
            Id = notification.Id,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt
        };
    }

    public async Task<int> MarkAllReadAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        var notifications = await _db.IncidentNotifications
            .Where(x => x.RecipientUserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return notifications.Count;
    }
}
