namespace MedSafe.Models;

public class IncidentNotification
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public int? NotificationTypeId { get; set; }
    public int? RecipientUserId { get; set; }
    public int? NotificationMethodId { get; set; }

    public bool IsAutomatic { get; set; } = true;
    public DateTime? SentAt { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
