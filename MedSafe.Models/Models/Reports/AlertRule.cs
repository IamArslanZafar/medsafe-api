namespace MedSafe.Models;

public class AlertRule
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TriggerCondition { get; set; } = string.Empty;
    public string TargetRoles { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public DateTime? LastTriggered { get; set; }
    public string? DeliveryConfig { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? MatchModeId { get; set; }
    public int? UrgencyId { get; set; }
    public string? NotificationTitle { get; set; }
    public string? NotificationMessage { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    public AlertRuleMatchMode? MatchMode { get; set; }
    public NotificationUrgency? NotificationUrgency { get; set; }
    public ICollection<AlertRuleCondition> Conditions { get; set; } = new List<AlertRuleCondition>();
    public ICollection<AlertRuleRecipient> Recipients { get; set; } = new List<AlertRuleRecipient>();
}
