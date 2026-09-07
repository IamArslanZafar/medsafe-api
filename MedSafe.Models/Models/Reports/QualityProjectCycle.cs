namespace MedSafe.Models;

// One PDSA (Plan-Do-Study-Act) cycle under a QualityProject — ported
// field-for-field from PDSACycleTab.jsx's emptyCycle(). Tester/Participants
// are antd `Select mode="tags"` fields (free-form string lists) and
// PredictionsJson is an array of {prediction, data} pairs — all stored as
// JSON since none need their own table.
public class QualityProjectCycle
{
    public int Id { get; set; }
    public int QualityProjectId { get; set; }
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
