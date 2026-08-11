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
}
