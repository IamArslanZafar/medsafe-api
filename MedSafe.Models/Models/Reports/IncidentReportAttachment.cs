namespace MedSafe.Models;

public class IncidentReportAttachment
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string StorageKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public int? DeletedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
}
