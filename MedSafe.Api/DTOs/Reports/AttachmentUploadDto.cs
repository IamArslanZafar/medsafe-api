namespace MedSafeAPI.DTOs;

// Shared response shape for both CpdActivitiesController's and
// ClinicalPharmacyInterventionsController's attachment upload endpoints —
// identical fields either way, so one DTO covers both.
public sealed class AttachmentUploadDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}
