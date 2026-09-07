namespace MedSafe.Models;

// A file uploaded from ClinicalPharmacyInterventionForm.jsx's "Attachments"
// picker (the form supports multiple files). Uploaded standalone (the
// ClinicalPharmacyIntervention doesn't exist yet at that point in the form)
// — the returned Id is what gets JSON-stringified into
// ClinicalPharmacyIntervention.AttachmentIdsJson.
public class ClinicalPharmacyInterventionAttachment
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public int? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
