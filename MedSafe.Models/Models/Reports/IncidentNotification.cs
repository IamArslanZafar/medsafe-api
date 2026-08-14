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
    public int? RecipientUserId { get; set; }
    public int? UrgencyId { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public bool IsAutomatic { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
    public AlertRule? AlertRule { get; set; }
    public User? RecipientUser { get; set; }
    public NotificationUrgency? Urgency { get; set; }
}
