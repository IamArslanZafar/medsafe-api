namespace MedSafe.Models;

// Research and Publications module — ported field-for-field from
// ResearchPublications.jsx's initialPublications array (currently a
// useState array, lost on refresh). PublicationDate and Identifier stay
// free-text strings, matching how the frontend already stores/displays
// them (e.g. "24 Aug 2026") — not parsed into a strict date/DOI type.
public class ResearchPublication
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? MetaLine { get; set; }
    public string PublicationType { get; set; } = string.Empty;
    public string? PublicationDate { get; set; }
    public string? Journal { get; set; }
    public string? Authors { get; set; }
    public string? Description { get; set; }
    public string? Identifier { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "Published";
    public string TagsJson { get; set; } = "[]";
    public int? SubmittedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
