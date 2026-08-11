using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class AlertRuleCreateDto
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string TriggerCondition { get; set; } = string.Empty;
    [Required] public string TargetRoles { get; set; } = string.Empty;   // JSON: ["Admin","Physician"]
    [Required] public string Urgency { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    public string? DeliveryConfig { get; set; }
}

public class AlertRuleResponseDto
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TriggerCondition { get; set; } = string.Empty;
    public string TargetRoles { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? LastTriggered { get; set; }
    public string? DeliveryConfig { get; set; }
}
