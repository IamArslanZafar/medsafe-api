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

    public ResearchPublicationsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
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
        return Ok(new ResearcherProfileDto { Orcid = profile?.Orcid ?? string.Empty });
    }

    [HttpPut("orcid")]
    public async Task<IActionResult> SaveOrcid(ResearcherProfileDto dto)
    {
        var profile = await _db.ResearcherProfiles.FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId);
        if (profile == null)
        {
            profile = new ResearcherProfile { UserId = _currentUser.UserId, Orcid = dto.Orcid };
            _db.ResearcherProfiles.Add(profile);
        }
        else
        {
            profile.Orcid = dto.Orcid;
        }

        await _db.SaveChangesAsync();
        return Ok(new ResearcherProfileDto { Orcid = profile.Orcid });
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
