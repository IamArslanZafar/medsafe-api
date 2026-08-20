using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/adr-severities")]
[Authorize]
public class AdrSeveritiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdrSeveritiesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.AdrSeverities.AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);

        var items = await query.OrderBy(x => x.DisplayOrder).ToListAsync();
        return Ok(items.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.AdrSeverities.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(AdrSeverityUpsertDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();

        if (await _db.AdrSeverities.AnyAsync(x => x.Code == code || x.Name == name))
            return Conflict(new { message = "An ADR severity with this code or name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.AdrSeverities.AnyAsync() ? await _db.AdrSeverities.MaxAsync(x => x.DisplayOrder) + 1 : 1);

        var item = new AdrSeverity
        {
            Code = code,
            Name = name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.AdrSeverities.Add(item);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, AdrSeverityUpsertDto dto)
    {
        var item = await _db.AdrSeverities.FindAsync(id);
        if (item == null) return NotFound();

        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();

        if (await _db.AdrSeverities.AnyAsync(x => x.Id != id && (x.Code == code || x.Name == name)))
            return Conflict(new { message = "An ADR severity with this code or name already exists" });

        item.Code = code;
        item.Name = name;
        item.Description = dto.Description;
        item.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) item.DisplayOrder = dto.DisplayOrder.Value;
        item.ModifiedBy = CurrentUserId();
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    // Soft delete — existing reports may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.AdrSeverities.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        item.ModifiedBy = CurrentUserId();
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "ADR severity deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(AdrSeverityBulkDeleteDto dto)
    {
        var items = await _db.AdrSeverities.Where(x => dto.Ids.Contains(x.Id)).ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.IsActive = false;
            item.ModifiedBy = userId;
            item.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = items.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static AdrSeverityDto MapToDto(AdrSeverity x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        IsActive = x.IsActive,
        DisplayOrder = x.DisplayOrder
    };
}
