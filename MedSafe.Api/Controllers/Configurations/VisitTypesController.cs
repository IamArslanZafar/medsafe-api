using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/visit-types")]
[Authorize]
public class VisitTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public VisitTypesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.VisitTypes.AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);

        var items = await query.OrderBy(x => x.DisplayOrder).ToListAsync();
        return Ok(items.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.VisitTypes.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(VisitTypeUpsertDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();

        if (await _db.VisitTypes.AnyAsync(x => x.Code == code || x.Name == name))
            return Conflict(new { message = "A visit type with this code or name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.VisitTypes.AnyAsync() ? await _db.VisitTypes.MaxAsync(x => x.DisplayOrder) + 1 : 1);

        var item = new VisitType
        {
            Code = code,
            Name = name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.VisitTypes.Add(item);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, VisitTypeUpsertDto dto)
    {
        var item = await _db.VisitTypes.FindAsync(id);
        if (item == null) return NotFound();

        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();

        if (await _db.VisitTypes.AnyAsync(x => x.Id != id && (x.Code == code || x.Name == name)))
            return Conflict(new { message = "A visit type with this code or name already exists" });

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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.VisitTypes.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        item.ModifiedBy = CurrentUserId();
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Visit type deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(VisitTypeBulkDeleteDto dto)
    {
        var items = await _db.VisitTypes.Where(x => dto.Ids.Contains(x.Id)).ToListAsync();

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

    private static VisitTypeDto MapToDto(VisitType x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        IsActive = x.IsActive,
        DisplayOrder = x.DisplayOrder
    };
}
