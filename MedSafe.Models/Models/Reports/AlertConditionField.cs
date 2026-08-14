namespace MedSafe.Models;

public class AlertConditionField
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // Tells the frontend rule builder where this field's pickable values come from
    // (e.g. a lookup table code, or "text" for a free-typed value) — see
    // AlertConditionFieldOperator for which operators are valid per field.
    public string ValueSourceCode { get; set; } = string.Empty;
    public bool IsMultiValue { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }

    public ICollection<AlertConditionFieldOperator> FieldOperators { get; set; } = new List<AlertConditionFieldOperator>();
}
