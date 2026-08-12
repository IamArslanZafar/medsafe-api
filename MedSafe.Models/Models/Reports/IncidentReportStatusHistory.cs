namespace MedSafe.Models;

public class IncidentReportStatusHistory
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = null!;
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; } = null!;

    public IncidentReport IncidentReport { get; set; } = null!;
}
