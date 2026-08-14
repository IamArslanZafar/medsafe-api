namespace MedSafe.Models;

public class AlertConditionOperator
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<AlertConditionFieldOperator> FieldOperators { get; set; } = new List<AlertConditionFieldOperator>();
}
