namespace MedSafeAPI.DTOs;

public sealed class NotificationListItemDto
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public string IncidentReportNumber { get; set; } = string.Empty;
    public int? AlertRuleId { get; set; }
    public string? RuleId { get; set; }
    public string? RuleName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? UrgencyId { get; set; }
    public string? UrgencyCode { get; set; }
    public string? UrgencyName { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class MyNotificationsResponse
{
    public List<NotificationListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class UnreadNotificationCountDto
{
    public int UnreadCount { get; set; }
}

public sealed class MarkNotificationReadResponse
{
    public int Id { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
