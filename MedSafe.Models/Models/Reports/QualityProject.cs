namespace MedSafe.Models;

// Quality Project Tracker module — the "charter" (ported field-for-field
// from QualityProjectTracker.jsx's emptyCharter()), currently stored in
// localStorage. Achievements/ProgressNotes are the "Progress History" tab's
// own two fields — that tab has no data of its own, it just edits these two
// fields on the project record itself.
public class QualityProject
{
    public int Id { get; set; }
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
