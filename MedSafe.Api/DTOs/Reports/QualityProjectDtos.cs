using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class QualityProjectDto
{
    public int Id { get; set; }
    public string? ProjectCode { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string? OrgName { get; set; }
    public string? SponsorName { get; set; }
    public string? AimStatement { get; set; }
    public string? Problem { get; set; }
    public string? Reason { get; set; }
    public string? Outcomes { get; set; }
    public string? OutcomeMeasures { get; set; }
    public string? ProcessMeasures { get; set; }
    public string? InitialActivities { get; set; }
    public string? Barriers { get; set; }
    public string? Stakeholders { get; set; }
    public string Status { get; set; } = "In Progress";
    public string? Achievements { get; set; }
    public string? ProgressNotes { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QualityProjectCycleDto> Cycles { get; set; } = new();
}

public class QualityProjectCreateDto
{
    [Required] public string ProjectTitle { get; set; } = string.Empty;
    public string? OrgName { get; set; }
    public string? SponsorName { get; set; }
    public string? AimStatement { get; set; }
    public string? Problem { get; set; }
    public string? Reason { get; set; }
    public string? Outcomes { get; set; }
    public string? OutcomeMeasures { get; set; }
    public string? ProcessMeasures { get; set; }
    public string? InitialActivities { get; set; }
    public string? Barriers { get; set; }
    public string? Stakeholders { get; set; }
    public string Status { get; set; } = "In Progress";
}

// Same fields as create — PUT replaces the whole charter, including
// Achievements/ProgressNotes (the Progress History tab's own two fields).
public sealed class QualityProjectUpdateDto : QualityProjectCreateDto
{
    public string? Achievements { get; set; }
    public string? ProgressNotes { get; set; }
}

public sealed class QualityProjectCycleDto
{
    public int Id { get; set; }
    public string? ChangeIdea { get; set; }
    public string TesterJson { get; set; } = "[]";
    public DateTime? Timeframe { get; set; }
    public string? Location { get; set; }
    public string ParticipantsJson { get; set; } = "[]";
    public string? LearningGoal { get; set; }
    public string PredictionsJson { get; set; } = "[]";
    public string? Observations { get; set; }
    public string? StudyResults { get; set; }
    public string? StudyLearning { get; set; }
    public string? ActPlan { get; set; }
    public string? Decision { get; set; }
    public string Status { get; set; } = "In Progress";
    public DateTime CreatedAt { get; set; }
}

// One upsert DTO for both "add a new cycle" and "edit an existing cycle" —
// PDSACycleTab.jsx already treats both as the same save action. Id = 0 (or
// omitted) means create; a non-zero Id that belongs to this project means update.
public sealed class QualityProjectCycleUpsertDto
{
    public int Id { get; set; }
    public string? ChangeIdea { get; set; }
    public string TesterJson { get; set; } = "[]";
    public DateTime? Timeframe { get; set; }
    public string? Location { get; set; }
    public string ParticipantsJson { get; set; } = "[]";
    public string? LearningGoal { get; set; }
    public string PredictionsJson { get; set; } = "[]";
    public string? Observations { get; set; }
    public string? StudyResults { get; set; }
    public string? StudyLearning { get; set; }
    public string? ActPlan { get; set; }
    public string? Decision { get; set; }
    public string Status { get; set; } = "In Progress";
}
