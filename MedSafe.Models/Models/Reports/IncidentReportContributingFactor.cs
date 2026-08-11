namespace MedSafe.Models;

public class IncidentReportContributingFactor
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public int ContributingFactorId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
