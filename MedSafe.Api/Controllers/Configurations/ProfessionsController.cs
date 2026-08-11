using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/professions")]
[Authorize]
public class ProfessionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfessionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.Professions.AsQueryable();
        if (!includeInactive) query = query.Where(p => p.IsActive);

        var professions = await query.OrderBy(p => p.DisplayOrder).ToListAsync();
        return Ok(professions.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var profession = await _db.Professions.FindAsync(id);
        if (profession == null) return NotFound();
        return Ok(MapToDto(profession));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(ProfessionUpsertDto dto)
    {
        if (await _db.Professions.AnyAsync(p => p.Name == dto.Name))
            return Conflict(new { message = "A profession with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.Professions.AnyAsync() ? await _db.Professions.MaxAsync(p => p.DisplayOrder) + 1 : 1);

        var profession = new Profession
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId(),
            CreatedDate = DateTime.UtcNow
        };

        _db.Professions.Add(profession);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(profession));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ProfessionUpsertDto dto)
    {
        var profession = await _db.Professions.FindAsync(id);
        if (profession == null) return NotFound();

        if (await _db.Professions.AnyAsync(p => p.Id != id && p.Name == dto.Name))
            return Conflict(new { message = "A profession with this name already exists" });

        profession.Name = dto.Name;
        profession.Description = dto.Description;
        profession.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) profession.DisplayOrder = dto.DisplayOrder.Value;
        profession.ModifiedBy = CurrentUserId();
        profession.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(profession));
    }

    // Soft delete — Users may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var profession = await _db.Professions.FindAsync(id);
        if (profession == null) return NotFound();

        profession.IsActive = false;
        profession.ModifiedBy = CurrentUserId();
        profession.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Profession deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(ProfessionBulkDeleteDto dto)
    {
        var professions = await _db.Professions
            .Where(p => dto.Ids.Contains(p.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var profession in professions)
        {
            profession.IsActive = false;
            profession.ModifiedBy = userId;
            profession.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = professions.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static ProfessionDto MapToDto(Profession p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        IsActive = p.IsActive,
        DisplayOrder = p.DisplayOrder
    };
}
