namespace MedSafe.Models;

public class Feedback
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public int? SubmittedByUserId { get; set; }
    public string? SubmittedByRole { get; set; }
    public string Status { get; set; } = "Pending Review";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
