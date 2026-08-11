using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/frequencies")]
[Authorize]
public class FrequenciesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FrequenciesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.Frequencies.AsQueryable();
        if (!includeInactive) query = query.Where(f => f.IsActive);

        var frequencies = await query.OrderBy(f => f.DisplayOrder).ToListAsync();
        return Ok(frequencies.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var frequency = await _db.Frequencies.FindAsync(id);
        if (frequency == null) return NotFound();
        return Ok(MapToDto(frequency));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(FrequencyUpsertDto dto)
    {
        if (await _db.Frequencies.AnyAsync(f => f.Name == dto.Name))
            return Conflict(new { message = "A frequency with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.Frequencies.AnyAsync() ? await _db.Frequencies.MaxAsync(f => f.DisplayOrder) + 1 : 1);

        var frequency = new Frequency
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.Frequencies.Add(frequency);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(frequency));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, FrequencyUpsertDto dto)
    {
        var frequency = await _db.Frequencies.FindAsync(id);
        if (frequency == null) return NotFound();

        if (await _db.Frequencies.AnyAsync(f => f.Id != id && f.Name == dto.Name))
            return Conflict(new { message = "A frequency with this name already exists" });

        frequency.Name = dto.Name;
        frequency.Description = dto.Description;
        frequency.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) frequency.DisplayOrder = dto.DisplayOrder.Value;
        frequency.ModifiedBy = CurrentUserId();
        frequency.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(frequency));
    }

    // Soft delete — IncidentReports.FrequencyId may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var frequency = await _db.Frequencies.FindAsync(id);
        if (frequency == null) return NotFound();

        frequency.IsActive = false;
        frequency.ModifiedBy = CurrentUserId();
        frequency.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Frequency deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(FrequencyBulkDeleteDto dto)
    {
        var frequencies = await _db.Frequencies
            .Where(f => dto.Ids.Contains(f.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var frequency in frequencies)
        {
            frequency.IsActive = false;
            frequency.ModifiedBy = userId;
            frequency.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = frequencies.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static FrequencyDto MapToDto(Frequency f) => new()
    {
        Id = f.Id,
        Name = f.Name,
        Description = f.Description,
        IsActive = f.IsActive,
        DisplayOrder = f.DisplayOrder
    };
}
