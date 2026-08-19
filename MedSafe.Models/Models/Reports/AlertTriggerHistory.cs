namespace MedSafe.Models;

// One row = one Alert Rule matching once for one Incident Report — independent of
// how many recipients that match fans out to (see IncidentNotification.AlertTriggerId).
// This is what the Alert Triggers Dashboard counts/charts/lists; IncidentNotification
// stays the per-recipient email-delivery record.
public class AlertTriggerHistory
{
    public long Id { get; set; }
    public string AlertTriggerNumber { get; set; } = null!;
    public int AlertRuleId { get; set; }
    public int IncidentReportId { get; set; }
    public int? UrgencyId { get; set; }
    public string TriggerSource { get; set; } = null!;
    public string? ConditionSummary { get; set; }
    public string? MatchedConditionSnapshot { get; set; }
    public string Status { get; set; } = "OPEN";
    public string DedupeKey { get; set; } = null!;
    public DateTime TriggeredAt { get; set; }
    public int? AcknowledgedByUserId { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public AlertRule AlertRule { get; set; } = null!;
    public IncidentReport IncidentReport { get; set; } = null!;
    public NotificationUrgency? Urgency { get; set; }
    public User? AcknowledgedByUser { get; set; }
    public User? ResolvedByUser { get; set; }
    public ICollection<IncidentNotification> Notifications { get; set; } = new List<IncidentNotification>();
}
