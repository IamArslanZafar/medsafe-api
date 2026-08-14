namespace MedSafe.Models;

// One recipient assigned to an alert rule — a rule can fan out to multiple
// users (each row is one recipient), matching the multiple-recipient requirement.
public class AlertRuleRecipient
{
    public int Id { get; set; }
    public int AlertRuleId { get; set; }
    public int RecipientTypeId { get; set; }
    public int RecipientUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ModifiedByUserId { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public AlertRule AlertRule { get; set; } = null!;
    public NotificationRecipientType RecipientType { get; set; } = null!;
    public User RecipientUser { get; set; } = null!;
}
