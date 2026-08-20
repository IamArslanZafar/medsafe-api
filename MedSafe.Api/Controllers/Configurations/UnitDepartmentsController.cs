using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/unit-departments")]
[Authorize]
public class UnitDepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public UnitDepartmentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.UnitDepartments.AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);

        var items = await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync();
        return Ok(items.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.UnitDepartments.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(UnitDepartmentUpsertDto dto)
    {
        var name = dto.Name.Trim();
        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim().ToUpperInvariant();

        if (await _db.UnitDepartments.AnyAsync(x => x.Name == name || (code != null && x.Code == code)))
            return Conflict(new { message = "A unit / department with this code or name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.UnitDepartments.AnyAsync() ? await _db.UnitDepartments.MaxAsync(x => x.DisplayOrder) + 1 : 1);

        var item = new UnitDepartment
        {
            Code = code,
            Name = name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.UnitDepartments.Add(item);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UnitDepartmentUpsertDto dto)
    {
        var item = await _db.UnitDepartments.FindAsync(id);
        if (item == null) return NotFound();

        var name = dto.Name.Trim();
        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim().ToUpperInvariant();

        if (await _db.UnitDepartments.AnyAsync(x => x.Id != id && (x.Name == name || (code != null && x.Code == code))))
            return Conflict(new { message = "A unit / department with this code or name already exists" });

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
        var item = await _db.UnitDepartments.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        item.ModifiedBy = CurrentUserId();
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Unit / department deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(UnitDepartmentBulkDeleteDto dto)
    {
        var items = await _db.UnitDepartments.Where(x => dto.Ids.Contains(x.Id)).ToListAsync();

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

    private static UnitDepartmentDto MapToDto(UnitDepartment x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        IsActive = x.IsActive,
        DisplayOrder = x.DisplayOrder
    };
}
