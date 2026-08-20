namespace MedSafe.Models;

// "Other Healthcare Professional Involved" — distinct from the reporter
// (JWT/Users table), a free-standing named professional entered on the form.
public class IncidentReportHealthcareProfessional
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public string Name { get; set; } = null!;
    public int ProfessionId { get; set; }
    public int PositionId { get; set; }
    public string? ContactNumber { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
