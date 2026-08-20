using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

// Sub-level under Unit/Department — e.g. "ICU" (unit) -> "Bay 3" (section).
// Optional on the Incident Report form; scoped to one parent unit like Position
// is scoped to a Profession.
[ApiController]
[Route("api/sections")]
[Authorize]
public class SectionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SectionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, [FromQuery] int? unitDepartmentId = null)
    {
        var query = _db.Sections.AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (unitDepartmentId.HasValue) query = query.Where(x => x.UnitDepartmentId == unitDepartmentId.Value);

        var items = await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync();
        return Ok(items.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Sections.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(SectionUpsertDto dto)
    {
        var name = dto.Name.Trim();

        if (!await _db.UnitDepartments.AnyAsync(u => u.Id == dto.UnitDepartmentId))
            return BadRequest(new { message = "Unit / department not found" });

        if (await _db.Sections.AnyAsync(x => x.Name == name && x.UnitDepartmentId == dto.UnitDepartmentId))
            return Conflict(new { message = "A section with this name already exists under this unit / department" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.Sections.AnyAsync() ? await _db.Sections.MaxAsync(x => x.DisplayOrder) + 1 : 1);

        var item = new Section
        {
            Name = name,
            Description = dto.Description,
            UnitDepartmentId = dto.UnitDepartmentId,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.Sections.Add(item);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, SectionUpsertDto dto)
    {
        var item = await _db.Sections.FindAsync(id);
        if (item == null) return NotFound();

        var name = dto.Name.Trim();

        if (!await _db.UnitDepartments.AnyAsync(u => u.Id == dto.UnitDepartmentId))
            return BadRequest(new { message = "Unit / department not found" });

        if (await _db.Sections.AnyAsync(x => x.Id != id && x.Name == name && x.UnitDepartmentId == dto.UnitDepartmentId))
            return Conflict(new { message = "A section with this name already exists under this unit / department" });

        item.Name = name;
        item.Description = dto.Description;
        item.UnitDepartmentId = dto.UnitDepartmentId;
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
        var item = await _db.Sections.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        item.ModifiedBy = CurrentUserId();
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Section deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(SectionBulkDeleteDto dto)
    {
        var items = await _db.Sections.Where(x => dto.Ids.Contains(x.Id)).ToListAsync();

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

    private static SectionDto MapToDto(Section x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Description = x.Description,
        UnitDepartmentId = x.UnitDepartmentId,
        IsActive = x.IsActive,
        DisplayOrder = x.DisplayOrder
    };
}
