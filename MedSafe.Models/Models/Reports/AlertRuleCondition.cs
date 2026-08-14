namespace MedSafe.Models;

// One condition row in a rule's builder (e.g. "Harm Level >= E") — the actual
// selected value(s) live in AlertRuleConditionValue, not here.
public class AlertRuleCondition
{
    public int Id { get; set; }
    public int AlertRuleId { get; set; }
    public int ConditionFieldId { get; set; }
    public int OperatorId { get; set; }
    public int DisplayOrder { get; set; }

    public AlertRule AlertRule { get; set; } = null!;
    public AlertConditionField ConditionField { get; set; } = null!;
    public AlertConditionOperator Operator { get; set; } = null!;

    public ICollection<AlertRuleConditionValue> Values { get; set; } = new List<AlertRuleConditionValue>();
}
