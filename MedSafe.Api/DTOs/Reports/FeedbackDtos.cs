using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class FeedbackDto
{
    public int Id { get; set; }
    // Display code shown on the Feedback screen (e.g. "FB-01") — derived from Id, not stored.
    public string ReferenceCode { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public string? SubmittedByRole { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class FeedbackCreateDto
{
    [Range(1, 5)] public int Rating { get; set; }
    [Required] public string Category { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}

public sealed class FeedbackStatusUpdateDto
{
    [Required] public string Status { get; set; } = string.Empty;
}
