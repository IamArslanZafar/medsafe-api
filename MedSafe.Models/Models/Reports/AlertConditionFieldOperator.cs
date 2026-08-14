namespace MedSafe.Models;

// Mapping table — which AlertConditionOperators are valid for a given
// AlertConditionField (composite key, see AppDbContext.OnModelCreating).
public class AlertConditionFieldOperator
{
    public int ConditionFieldId { get; set; }
    public int OperatorId { get; set; }

    public AlertConditionField ConditionField { get; set; } = null!;
    public AlertConditionOperator Operator { get; set; } = null!;
}
