namespace MedSafe.Models;

public class IncidentReportSeriousnessCriterion
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public int SeriousnessCriterionId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
