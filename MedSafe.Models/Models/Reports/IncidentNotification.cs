namespace MedSafe.Models;

public class IncidentNotification
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public int NotificationTypeId { get; set; }
    public string PersonName { get; set; } = null!;
    public DateTime NotifiedAt { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
