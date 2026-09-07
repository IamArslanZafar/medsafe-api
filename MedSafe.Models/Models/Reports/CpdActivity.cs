namespace MedSafe.Models;

// CPD & Education module — ported field-for-field from CPDActivityForm.jsx
// (whose submit handler currently does nothing persistent at all) and the
// shape implied by CPDActivityDashboard.jsx's hardcoded ACTIVITIES sample
// data (ReferenceCode, Credits, Status, ReviewFeedback). "ActivityType"
// (not "Type") to avoid colliding with the C# `Type` keyword — the
// frontend's own field is just called "type".
public class CpdActivity
{
    public int Id { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime ActivityDate { get; set; }
    public decimal Hours { get; set; }
    public decimal Credits { get; set; }
    public string Reflection { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public int? AttachmentId { get; set; }
    // "draft" | "pending" | "approved" | "evidence" — approved/evidence are
    // set by a future review workflow, not by any endpoint that exists yet.
    public string Status { get; set; } = "pending";
    public string? ReviewFeedback { get; set; }
    public int? SubmittedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
