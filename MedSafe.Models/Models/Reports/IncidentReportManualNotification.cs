namespace MedSafe.Models;

// A manual "who was personally told about this incident" log entered on the form
// itself — distinct from IncidentNotification, which is the automated alert-rule
// notification system.
public class IncidentReportManualNotification
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public string TypeOfPersonNotified { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime NotifiedAt { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
