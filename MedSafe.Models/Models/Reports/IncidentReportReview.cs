namespace MedSafe.Models;

public class IncidentReportReview
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public int ReviewerUserId { get; set; }
    public string? ClinicalAssessmentNote { get; set; }
    public string? FollowUpActions { get; set; }
    public int? ActionOwnerUserId { get; set; }
    public string ResolutionStatus { get; set; } = "Open";
    public DateTime StartedAt { get; set; }
    public DateTime? SignedOffAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
