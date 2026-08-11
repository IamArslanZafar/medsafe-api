namespace MedSafe.Models;

public class IncidentReportCurrentMedication
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public int CurrentMedicationId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
