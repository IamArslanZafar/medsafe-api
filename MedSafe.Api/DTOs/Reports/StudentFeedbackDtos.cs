using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class StudentFeedbackDto
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
    public int? SubmittedByUserId { get; set; }
    public string? SubmittedByRole { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class StudentFeedbackCreateDto
{
    [Required] public string StudentName { get; set; } = string.Empty;
    [Range(1, 5)] public int OverallQuality { get; set; }
    [Range(1, 5)] public int InstructorClarity { get; set; }
    public string InstructorRatingsJson { get; set; } = "{}";
    public string? InstructorComments { get; set; }
    [Required] public string MaterialsSufficient { get; set; } = string.Empty;
    public string? MaterialsComments { get; set; }
    [Range(1, 5)] public int EnvironmentRating { get; set; }
    public string SupportServicesJson { get; set; } = "{}";
}
