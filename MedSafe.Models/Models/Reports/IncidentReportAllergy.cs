namespace MedSafe.Models;

public class IncidentReportAllergy
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public int AllergyId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
