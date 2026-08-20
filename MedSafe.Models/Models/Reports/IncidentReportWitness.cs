namespace MedSafe.Models;

public class IncidentReportWitness
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
