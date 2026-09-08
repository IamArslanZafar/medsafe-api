using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

// Backs the "Research and Publications" module's publications list + ORCID
// field. DOI/PubMed lookup stays a frontend-only demo (MOCK_DOI_DB/
// MOCK_PMID_DB) — no endpoint for it here, nothing to proxy.
[ApiController]
[Route("api/research-publications")]
[Authorize]
public class ResearchPublicationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public ResearchPublicationsController(AppDbContext db, ICurrentUserService currentUser, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _db = db;
        _currentUser = currentUser;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.ResearchPublications.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(list.Select(MapToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ResearchPublicationCreateDto dto)
    {
        var publication = new ResearchPublication
        {
            Title = dto.Title,
            MetaLine = dto.MetaLine,
            PublicationType = dto.PublicationType,
            PublicationDate = dto.PublicationDate,
            Journal = dto.Journal,
            Authors = dto.Authors,
            Description = dto.Description,
            Identifier = dto.Identifier,
            Source = dto.Source,
            Status = dto.Status,
            TagsJson = dto.TagsJson,
            SubmittedByUserId = _currentUser.UserId,
        };

        _db.ResearchPublications.Add(publication);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(publication));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ResearchPublicationCreateDto dto)
    {
        var publication = await _db.ResearchPublications.FindAsync(id);
        if (publication == null) return NotFound();

        publication.Title = dto.Title;
        publication.MetaLine = dto.MetaLine;
        publication.PublicationType = dto.PublicationType;
        publication.PublicationDate = dto.PublicationDate;
        publication.Journal = dto.Journal;
        publication.Authors = dto.Authors;
        publication.Description = dto.Description;
        publication.Identifier = dto.Identifier;
        publication.Source = dto.Source;
        publication.Status = dto.Status;
        publication.TagsJson = dto.TagsJson;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(publication));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var publication = await _db.ResearchPublications.FindAsync(id);
        if (publication == null) return NotFound();

        _db.ResearchPublications.Remove(publication);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Publication deleted" });
    }

    [HttpGet("orcid")]
    public async Task<IActionResult> GetOrcid()
    {
        var profile = await _db.ResearcherProfiles.FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId);
        return Ok(new ResearcherProfileDto { Orcid = profile?.Orcid ?? string.Empty, Name = profile?.Name, Verified = profile?.Verified ?? false });
    }

    // Manual typed entry (the pre-existing Save button) — always unverified,
    // since it didn't come back from ORCID's own token endpoint.
    [HttpPut("orcid")]
    public async Task<IActionResult> SaveOrcid(ResearcherProfileDto dto)
    {
        var profile = await _db.ResearcherProfiles.FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId);
        if (profile == null)
        {
            profile = new ResearcherProfile { UserId = _currentUser.UserId, Orcid = dto.Orcid, Verified = false };
            _db.ResearcherProfiles.Add(profile);
        }
        else
        {
            profile.Orcid = dto.Orcid;
            profile.Name = null;
            profile.Verified = false;
        }

        await _db.SaveChangesAsync();
        return Ok(new ResearcherProfileDto { Orcid = profile.Orcid, Name = profile.Name, Verified = profile.Verified });
    }

    // POST /api/research-publications/orcid/exchange { code } — the real ORCID
    // OAuth "Connect" flow. The frontend sent the user to ORCID's authorize page
    // and got redirected back with a one-time authorization code; this exchanges
    // it server-side for an access token + the caller's verified ORCID iD (the
    // client secret never reaches the frontend, only this call uses it).
    [HttpPost("orcid/exchange")]
    public async Task<IActionResult> ExchangeOrcidCode(OrcidExchangeRequestDto dto)
    {
        var client = _httpClientFactory.CreateClient();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _config["Orcid:ClientId"] ?? string.Empty,
            ["client_secret"] = _config["Orcid:ClientSecret"] ?? string.Empty,
            ["grant_type"] = "authorization_code",
            ["code"] = dto.Code,
            ["redirect_uri"] = _config["Orcid:RedirectUri"] ?? string.Empty,
        });
        form.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, _config["Orcid:TokenUrl"]) { Content = form };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage orcidResponse;
        string rawBody;
        try
        {
            orcidResponse = await client.SendAsync(request);
            rawBody = await orcidResponse.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            // Network-level failure reaching orcid.org — surface it instead of a bare 500,
            // so the frontend toast tells us exactly what happened.
            return BadRequest(new { message = $"Could not reach ORCID: {ex.Message}" });
        }

        OrcidTokenResponse? token = null;
        try
        {
            token = System.Text.Json.JsonSerializer.Deserialize<OrcidTokenResponse>(rawBody);
        }
        catch (Exception)
        {
            // ORCID returned something that isn't JSON (e.g. an HTML error page) —
            // rawBody is still surfaced below so the real cause is visible.
        }

        if (!orcidResponse.IsSuccessStatusCode || token == null || string.IsNullOrWhiteSpace(token.Orcid))
        {
            var reason = token?.ErrorDescription ?? token?.Error ?? (string.IsNullOrWhiteSpace(rawBody) ? "ORCID did not return a valid connection." : rawBody);
            return BadRequest(new { message = $"ORCID exchange failed ({(int)orcidResponse.StatusCode}): {reason}" });
        }

        var profile = await _db.ResearcherProfiles.FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId);
        if (profile == null)
        {
            profile = new ResearcherProfile { UserId = _currentUser.UserId };
            _db.ResearcherProfiles.Add(profile);
        }
        profile.Orcid = token.Orcid;
        profile.Name = token.Name;
        profile.Verified = true;

        await _db.SaveChangesAsync();
        return Ok(new ResearcherProfileDto { Orcid = profile.Orcid, Name = profile.Name, Verified = profile.Verified });
    }

    private static ResearchPublicationDto MapToDto(ResearchPublication p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        MetaLine = p.MetaLine,
        PublicationType = p.PublicationType,
        PublicationDate = p.PublicationDate,
        Journal = p.Journal,
        Authors = p.Authors,
        Description = p.Description,
        Identifier = p.Identifier,
        Source = p.Source,
        Status = p.Status,
        TagsJson = p.TagsJson,
        CreatedAt = p.CreatedAt,
    };
}
