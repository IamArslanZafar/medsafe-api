using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public sealed class AlertTriggerFilterRequest
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int? UrgencyId { get; set; }
    public int? RuleId { get; set; }
    public int? RecipientUserId { get; set; }
    public int? NotificationMethodId { get; set; }
    public string? ReportType { get; set; }
    public int? ConditionFieldId { get; set; }
    public string? TriggerSource { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public interface IAlertDashboardService
{
    Task<AlertDashboardOverviewDto> GetOverviewAsync(DateTime? from, DateTime? to, string? reportType, CancellationToken cancellationToken);
    Task<AlertTriggerListResponse> GetTriggersAsync(AlertTriggerFilterRequest request, CancellationToken cancellationToken);
    Task<AlertTriggerDetailDto?> GetTriggerDetailAsync(long id, CancellationToken cancellationToken);
    Task<AlertDashboardFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken);
    Task<AlertTriggerStatusResponseDto?> AcknowledgeAsync(long id, int userId, CancellationToken cancellationToken);
    Task<AlertTriggerStatusResponseDto?> ResolveAsync(long id, int userId, CancellationToken cancellationToken);
    Task<List<AlertTriggerExportRowDto>> GetExportRowsAsync(AlertTriggerFilterRequest request, CancellationToken cancellationToken);
}

// One row per recipient notification — CSV export flattens the trigger/notification
// fan-out so "who got the email" is visible per line, matching the doc's column set.
public sealed class AlertTriggerExportRowDto
{
    public string AlertId { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string ReportNumber { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string? ConditionBase { get; set; }
    public string? Urgency { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Method { get; set; }
    public string? DeliveryStatus { get; set; }
    public DateTime? SentAt { get; set; }
    public string AlertStatus { get; set; } = string.Empty;
}
