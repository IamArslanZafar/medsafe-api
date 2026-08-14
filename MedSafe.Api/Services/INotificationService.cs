using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface INotificationService
{
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken);

    Task<MyNotificationsResponse> GetMyNotificationsAsync(bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken);

    Task<MarkNotificationReadResponse?> MarkReadAsync(int notificationId, CancellationToken cancellationToken);

    Task<int> MarkAllReadAsync(CancellationToken cancellationToken);
}
