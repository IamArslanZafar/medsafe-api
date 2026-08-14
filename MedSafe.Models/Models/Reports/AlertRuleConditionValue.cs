namespace MedSafe.Models;

// The actual selected value for a condition — either a lookup row id
// (LookupValueId, e.g. an ErrorCategory.Id) or a free-typed value (TextValue,
// e.g. "E" for a Harm Level comparison), depending on the field's ValueSourceCode.
public class AlertRuleConditionValue
{
    public int Id { get; set; }
    public int AlertRuleConditionId { get; set; }
    public int? LookupValueId { get; set; }
    public string? TextValue { get; set; }
    public int DisplayOrder { get; set; }

    public AlertRuleCondition AlertRuleCondition { get; set; } = null!;
}
