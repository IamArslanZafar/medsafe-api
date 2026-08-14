namespace MedSafe.Models;

public class AlertRuleMatchMode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
}
