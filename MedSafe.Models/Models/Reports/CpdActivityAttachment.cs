namespace MedSafe.Models;

// A file uploaded from CPDActivityForm.jsx's "Supporting Evidence" picker.
// Uploaded standalone (the CpdActivity doesn't exist yet at that point in the
// form) — the returned Id is what CpdActivity.AttachmentId then points to.
public class CpdActivityAttachment
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
