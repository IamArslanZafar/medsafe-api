namespace MedSafeAPI.DTOs;

// ── Alert Triggers Dashboard (Admin-only monitoring/analytics screen, separate
// from Alert Rules configuration) — backed by AlertTriggerHistory. ──

public sealed class AlertTrendPointDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public sealed class AlertStatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class AlertReportTypeCountDto
{
    public string ReportType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class AlertRuleCountDto
{
    public int AlertRuleId { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class NotificationChannelCountDto
{
    public string MethodCode { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class AlertUrgencyCountDto
{
    public int UrgencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class AlertTopRecipientDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Count { get; set; }
}

public sealed class AlertDashboardOverviewDto
{
    public int TotalRules { get; set; }
    public int ActiveRules { get; set; }
    public int InactiveRules { get; set; }
    // Rules whose own notification urgency is Immediate/Escalated — a rule
    // configuration property, unrelated to any individual trigger's urgency.
    public int ImmediateEscalatedRules { get; set; }
    // Inferred from each rule's own REPORT_TYPE condition value(s), not from
    // trigger history — a rule with no REPORT_TYPE condition counts in neither.
    public int MedicationErrorRules { get; set; }
    public int AdrRules { get; set; }
    public int CriticalAlerts { get; set; }
    public int AlertsTriggered { get; set; }
    public int NotificationsSent { get; set; }
    public int UniqueRecipients { get; set; }
    public List<AlertTrendPointDto> AlertsOverTime { get; set; } = [];
    // Same day/hour bucketing as AlertsOverTime, for the KPI card sparklines.
    public List<int> CriticalAlertsTrend { get; set; } = [];
    public List<int> NotificationsSentTrend { get; set; } = [];
    public List<int> UniqueRecipientsTrend { get; set; } = [];
    public List<AlertStatusCountDto> AlertsByStatus { get; set; } = [];
    public List<AlertReportTypeCountDto> AlertsByReportType { get; set; } = [];
    public List<AlertRuleCountDto> AlertsByRule { get; set; } = [];
    public List<NotificationChannelCountDto> NotificationsByChannel { get; set; } = [];
    public List<AlertUrgencyCountDto> AlertsByUrgency { get; set; } = [];
    public List<AlertTopRecipientDto> TopRecipients { get; set; } = [];
    // Rule-configuration data (which fields rules are built on) — not scoped to
    // the date/report-type filters above, same convention as DashboardService's
    // Overall Dashboard FieldUsage.
    public List<AlertRuleFieldUsageDto> FieldUsage { get; set; } = [];
}

// ── Trigger table ──

public sealed class AlertTriggerRuleRefDto
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertTriggerReportRefDto
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
}

public sealed class AlertTriggerUrgencyRefDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertTriggerNotificationSummaryDto
{
    public int Sent { get; set; }
    public int Pending { get; set; }
    public int Failed { get; set; }
}

public sealed class AlertTriggerListItemDto
{
    public long Id { get; set; }
    public string AlertId { get; set; } = string.Empty;
    public AlertTriggerRuleRefDto Rule { get; set; } = null!;
    public AlertTriggerReportRefDto Report { get; set; } = null!;
    public AlertTriggerUrgencyRefDto? Urgency { get; set; }
    public string TriggerSource { get; set; } = string.Empty;
    public string? ConditionBase { get; set; }
    public DateTime TriggeredAt { get; set; }
    public int RecipientCount { get; set; }
    public List<string> Channels { get; set; } = [];
    public AlertTriggerNotificationSummaryDto NotificationSummary { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}

public sealed class AlertTriggerListResponse
{
    public List<AlertTriggerListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// ── Trigger detail ──

public sealed class AlertTriggerDetailRuleDto
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? MatchMode { get; set; }
}

public sealed class AlertTriggerDetailReportDto
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string ReportStatus { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public sealed class AlertTriggerDetailTriggerDto
{
    public string Source { get; set; } = string.Empty;
    public string? ConditionSummary { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class MatchedConditionDto
{
    public string Field { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string? ActualValue { get; set; }
    public bool Matched { get; set; }
}

public sealed class AlertTriggerNotificationDetailDto
{
    public int NotificationId { get; set; }
    public int? RecipientUserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? RecipientType { get; set; }
    public string? Method { get; set; }
    public string? DeliveryStatus { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public int EmailAttemptCount { get; set; }
    public DateTime? LastEmailAttemptAt { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class AlertTriggerDetailDto
{
    public long Id { get; set; }
    public string AlertId { get; set; } = string.Empty;
    public AlertTriggerDetailRuleDto Rule { get; set; } = null!;
    public AlertTriggerDetailReportDto Report { get; set; } = null!;
    public AlertTriggerDetailTriggerDto Trigger { get; set; } = null!;
    public List<MatchedConditionDto> MatchedConditions { get; set; } = [];
    public List<AlertTriggerNotificationDetailDto> Notifications { get; set; } = [];
}

// ── Filter options ──

public sealed class AlertDashboardFilterRuleDto
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertDashboardFilterRecipientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class AlertDashboardFilterUrgencyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertDashboardFilterMethodDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertDashboardFilterSourceDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertDashboardFilterConditionFieldDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertDashboardFilterOptionsDto
{
    public List<AlertDashboardFilterRuleDto> Rules { get; set; } = [];
    public List<AlertDashboardFilterRecipientDto> Recipients { get; set; } = [];
    public List<AlertDashboardFilterUrgencyDto> Urgencies { get; set; } = [];
    public List<AlertDashboardFilterMethodDto> Methods { get; set; } = [];
    public List<string> Statuses { get; set; } = [];
    public List<string> DeliveryStatuses { get; set; } = [];
    public List<string> ReportTypes { get; set; } = [];
    public List<AlertDashboardFilterSourceDto> TriggerSources { get; set; } = [];
    public List<AlertDashboardFilterConditionFieldDto> ConditionFields { get; set; } = [];
}

public sealed class AlertTriggerStatusResponseDto
{
    public long Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public sealed class ResolveAlertTriggerRequest
{
    public string? Reason { get; set; }
}
