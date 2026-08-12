using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class SignOffReviewRequest
{
    [Required]
    [MinLength(3)]
    public string ClinicalAssessmentNote { get; set; } = string.Empty;

    public string? FollowUpActions { get; set; }
    public int? ActionOwnerUserId { get; set; }
}

public sealed class StartReviewResponse
{
    public int ReviewId { get; set; }
    public int IncidentReportId { get; set; }
    public string IncidentReportNumber { get; set; } = string.Empty;
    public int ReviewerUserId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string ReportStatus { get; set; } = string.Empty;
    public string ResolutionStatus { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

public sealed class SignOffReviewResponse
{
    public int ReviewId { get; set; }
    public int IncidentReportId { get; set; }
    public string IncidentReportNumber { get; set; } = string.Empty;
    public int ReviewerUserId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int? ActionOwnerUserId { get; set; }
    public string? ActionOwnerName { get; set; }
    public string ReportStatus { get; set; } = string.Empty;
    public string ResolutionStatus { get; set; } = string.Empty;
    public DateTime? SignedOffAt { get; set; }
}

public sealed class IncidentReportReviewDto
{
    public int ReviewId { get; set; }
    public int IncidentReportId { get; set; }
    public string IncidentReportNumber { get; set; } = string.Empty;
    public int ReviewerUserId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? ClinicalAssessmentNote { get; set; }
    public string? FollowUpActions { get; set; }
    public int? ActionOwnerUserId { get; set; }
    public string? ActionOwnerName { get; set; }
    public string ResolutionStatus { get; set; } = string.Empty;
    public string ReportStatus { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? SignedOffAt { get; set; }
}

public sealed class ActionOwnerOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Unit { get; set; }
}
