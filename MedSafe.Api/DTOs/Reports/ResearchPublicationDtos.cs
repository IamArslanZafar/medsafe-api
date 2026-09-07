using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class ResearchPublicationDto
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
    public DateTime CreatedAt { get; set; }
}

public sealed class ResearchPublicationCreateDto
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? MetaLine { get; set; }
    [Required] public string PublicationType { get; set; } = string.Empty;
    public string? PublicationDate { get; set; }
    public string? Journal { get; set; }
    public string? Authors { get; set; }
    public string? Description { get; set; }
    public string? Identifier { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "Published";
    public string TagsJson { get; set; } = "[]";
}

public sealed class ResearcherProfileDto
{
    public string Orcid { get; set; } = string.Empty;
}
