namespace MedSafe.Models;

// "Reported By" — the reference form allows more than one person to be logged as
// having reported the incident, distinct from SubmittedByUserId (the logged-in
// account that actually filled out and submitted the form).
public class IncidentReportReporter
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public string Name { get; set; } = null!;
    public DateTime ReportedDate { get; set; }
    public int? ProfessionId { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
