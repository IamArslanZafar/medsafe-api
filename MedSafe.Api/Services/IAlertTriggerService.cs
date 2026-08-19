namespace MedSafeAPI.Services;

public sealed class ConditionEvaluationResult
{
    public string FieldCode { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public string OperatorCode { get; set; } = null!;
    public string ExpectedValue { get; set; } = null!;
    public string? ActualValue { get; set; }
    public bool Matched { get; set; }
}

public sealed class AlertTriggerRecipientCommand
{
    public int UserId { get; set; }
    public int RecipientTypeId { get; set; }
}

public sealed class CreateAlertTriggerCommand
{
    public int AlertRuleId { get; set; }
    public int IncidentReportId { get; set; }
    public int? UrgencyId { get; set; }
    public string TriggerSource { get; set; } = null!;
    public string? ConditionSummary { get; set; }
    public string? MatchedConditionSnapshot { get; set; }
    public string DedupeKey { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public int CreatedByUserId { get; set; }
    public List<AlertTriggerRecipientCommand> Recipients { get; set; } = [];
}

// Central place both AlertRuleEvaluationService (submission-time rules) and
// AlertMonitorService (AR-005 48h reminder) go through to record a trigger —
// keeps "one rule matched once" (AlertTriggerHistory) and "N people were emailed
// about it" (IncidentNotification) from being conflated.
public interface IAlertTriggerService
{
    // Returns null (no-op) when DedupeKey already exists — callers already run
    // their own "does a notification exist for this report+rule+recipient"
    // check beforehand, this is a second, trigger-level backstop.
    Task<long?> CreateAsync(CreateAlertTriggerCommand command, CancellationToken cancellationToken);
}
