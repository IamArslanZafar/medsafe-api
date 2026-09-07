namespace MedSafe.Models;

// Student Feedback module — ported field-for-field from the frontend's
// StudentFeedbackForm.jsx (currently an in-memory mock store, lost on
// refresh). InstructorRatingsJson/SupportServicesJson hold small fixed-shape
// objects ({knowledgeable, prepared, participation, timelyFeedback} and
// {academicAdvising, technicalSupport} respectively) serialized as JSON —
// simplest option for a handful of sub-fields that don't need their own table.
public class StudentFeedback
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int OverallQuality { get; set; }
    public int InstructorClarity { get; set; }
    public string InstructorRatingsJson { get; set; } = "{}";
    public string? InstructorComments { get; set; }
    public string MaterialsSufficient { get; set; } = string.Empty;
    public string? MaterialsComments { get; set; }
    public int EnvironmentRating { get; set; }
    public string SupportServicesJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
