namespace MedSafe.Models;

public class IncidentNotification
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public int NotificationTypeId { get; set; }
    public string PersonName { get; set; } = null!;
    public DateTime NotifiedAt { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    // ── Alert Rule automatic-notification fields ──
    public int? AlertRuleId { get; set; }
    // Which single AlertTriggerHistory this per-recipient email fanned out from —
    // null for rows created before AlertTriggerHistory existed.
    public long? AlertTriggerId { get; set; }
    public int? RecipientUserId { get; set; }
    public int? UrgencyId { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public bool IsAutomatic { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // ── Email delivery fields ──
    public int? NotificationMethodId { get; set; }
    public string? Status { get; set; }
    public DateTime? SentAt { get; set; }
    public string? Notes { get; set; }
    public int EmailAttemptCount { get; set; }
    public DateTime? LastEmailAttemptAt { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
    public AlertRule? AlertRule { get; set; }
    public AlertTriggerHistory? AlertTrigger { get; set; }
    public User? RecipientUser { get; set; }
    public NotificationUrgency? Urgency { get; set; }
    public NotificationMethod? NotificationMethod { get; set; }
}
