using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class CpdActivityDto
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
    public string Status { get; set; } = string.Empty;
    public string? ReviewFeedback { get; set; }
    public int? SubmittedByUserId { get; set; }
    public string? SubmittedByRole { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CpdActivityCreateDto
{
    [Required] public string Category { get; set; } = string.Empty;
    [Required] public string Subcategory { get; set; } = string.Empty;
    [Required] public string Activity { get; set; } = string.Empty;
    [Required] public string ActivityType { get; set; } = string.Empty;
    [Required] public string Location { get; set; } = string.Empty;
    [Required] public string Format { get; set; } = string.Empty;
    [Required] public string Title { get; set; } = string.Empty;
    public DateTime ActivityDate { get; set; }
    public decimal Hours { get; set; }
    [Required] public string Reflection { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public int? AttachmentId { get; set; }
    // true = "Save Draft" button, false = "Submit" button (CPDActivityForm.jsx).
    public bool SaveAsDraft { get; set; }
}
