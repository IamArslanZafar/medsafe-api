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

    // Admin (or a user granted "Admin Data Access") sees every publication; everyone
    // else sees only what they themselves submitted — same rule as GET /incident-reports.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _db.ResearchPublications.AsQueryable();
        if (_currentUser.Role != "Admin" && !_currentUser.HasFullDataAccess)
            query = query.Where(p => p.SubmittedByUserId == _currentUser.UserId);

        var list = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(list.Select(MapToDto));
    }

    // True for Admin, a user granted "Admin Data Access", or the publication's own
    // submitter — guards Update/Delete below so an id can't be edited or removed by
    // a user it doesn't belong to just by knowing/guessing its numeric id.
    private bool CanAccessPublication(ResearchPublication publication) =>
        _currentUser.Role == "Admin" || _currentUser.HasFullDataAccess || publication.SubmittedByUserId == _currentUser.UserId;

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
        if (!CanAccessPublication(publication)) return Forbid();

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
        if (!CanAccessPublication(publication)) return Forbid();

        _db.ResearchPublications.Remove(publication);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Publication deleted" });
    }

    [HttpGet("orcid")]
    public async Task<IActionResult> GetOrcid()
    {
        var profile = await _db.ResearcherProfiles.FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId);
        // Every time the frontend checks the ORCID connection (Research & Publications
        // page load), quietly renew the stored access token if it's expired or close to
        // it — keeps the connection alive without the user ever re-clicking "Connect".
        if (profile != null) await RefreshOrcidTokenIfNeededAsync(profile);
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
            // A manually-typed iD replaces whatever OAuth connection was here before
            // (it may not even be the same ORCID account) — drop the now-stale tokens
            // rather than leaving them around to be silently refreshed against a
            // different iD than what's now on the profile.
            profile.AccessToken = null;
            profile.RefreshToken = null;
            profile.TokenExpiresAt = null;
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
        // Persist the connection itself, not just the identity it verified — lets
        // RefreshOrcidTokenIfNeededAsync keep calling ORCID on this user's behalf
        // later without making them click "Connect" again every time the token expires.
        profile.AccessToken = token.AccessToken;
        profile.RefreshToken = token.RefreshToken;
        profile.TokenExpiresAt = token.ExpiresIn.HasValue ? DateTime.UtcNow.AddSeconds(token.ExpiresIn.Value) : null;

        await _db.SaveChangesAsync();
        return Ok(new ResearcherProfileDto { Orcid = profile.Orcid, Name = profile.Name, Verified = profile.Verified });
    }

    // Renews the stored ORCID access token via the refresh_token grant once it's
    // expired or within 60 seconds of expiring — called from GetOrcid() so the
    // connection stays usable indefinitely without the user re-connecting. A no-op
    // when there's nothing to refresh (never connected, or connected via the manual
    // typed-iD path which never had a token). Failures here are swallowed rather
    // than surfaced: token freshness is a background concern, and the next call
    // just tries again with whatever token is still on file.
    private async Task RefreshOrcidTokenIfNeededAsync(ResearcherProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.RefreshToken)) return;
        if (profile.TokenExpiresAt.HasValue && profile.TokenExpiresAt.Value > DateTime.UtcNow.AddSeconds(60)) return;

        var client = _httpClientFactory.CreateClient();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _config["Orcid:ClientId"] ?? string.Empty,
            ["client_secret"] = _config["Orcid:ClientSecret"] ?? string.Empty,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = profile.RefreshToken,
        });
        form.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, _config["Orcid:TokenUrl"]) { Content = form };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await client.SendAsync(request);
            var rawBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return;

            var refreshed = System.Text.Json.JsonSerializer.Deserialize<OrcidTokenResponse>(rawBody);
            if (refreshed == null || string.IsNullOrWhiteSpace(refreshed.AccessToken)) return;

            profile.AccessToken = refreshed.AccessToken;
            // ORCID doesn't always rotate the refresh token on renewal — keep the
            // existing one if the response didn't include a new one.
            if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken)) profile.RefreshToken = refreshed.RefreshToken;
            profile.TokenExpiresAt = refreshed.ExpiresIn.HasValue ? DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn.Value) : null;
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Network hiccup reaching orcid.org — leave the stale token in place.
        }
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
