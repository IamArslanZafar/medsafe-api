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
    public string? Name { get; set; }
    public bool Verified { get; set; }
}

// Body of POST /api/research-publications/orcid/exchange — the authorization
// code ORCID redirected back to the frontend with (?code=...).
public sealed class OrcidExchangeRequestDto
{
    [Required] public string Code { get; set; } = string.Empty;
}

// What ORCID's own token endpoint returns — deserialized straight off its JSON
// response (snake_case field names), read only for the fields we need.
public sealed class OrcidTokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("orcid")]
    public string? Orcid { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
