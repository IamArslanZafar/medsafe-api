using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/dose-units")]
[Authorize]
public class DoseUnitsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoseUnitsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.DoseUnits.AsQueryable();
        if (!includeInactive) query = query.Where(u => u.IsActive);

        var units = await query.OrderBy(u => u.DisplayOrder).ToListAsync();
        return Ok(units.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var unit = await _db.DoseUnits.FindAsync(id);
        if (unit == null) return NotFound();
        return Ok(MapToDto(unit));
    }

    // Open to any logged-in role — the incident report form's medication fields
    // let a user add a new Dose Unit inline while filling out a report.
    // Update/Delete stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(DoseUnitUpsertDto dto)
    {
        if (await _db.DoseUnits.AnyAsync(u => u.Name == dto.Name))
            return Conflict(new { message = "A dose unit with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.DoseUnits.AnyAsync() ? await _db.DoseUnits.MaxAsync(u => u.DisplayOrder) + 1 : 1);

        var unit = new DoseUnit
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.DoseUnits.Add(unit);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(unit));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, DoseUnitUpsertDto dto)
    {
        var unit = await _db.DoseUnits.FindAsync(id);
        if (unit == null) return NotFound();

        if (await _db.DoseUnits.AnyAsync(u => u.Id != id && u.Name == dto.Name))
            return Conflict(new { message = "A dose unit with this name already exists" });

        unit.Name = dto.Name;
        unit.Description = dto.Description;
        unit.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) unit.DisplayOrder = dto.DisplayOrder.Value;
        unit.ModifiedBy = CurrentUserId();
        unit.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(unit));
    }

    // Soft delete — IncidentReports.DoseUnitId may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await _db.DoseUnits.FindAsync(id);
        if (unit == null) return NotFound();

        unit.IsActive = false;
        unit.ModifiedBy = CurrentUserId();
        unit.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Dose unit deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(DoseUnitBulkDeleteDto dto)
    {
        var units = await _db.DoseUnits
            .Where(u => dto.Ids.Contains(u.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var unit in units)
        {
            unit.IsActive = false;
            unit.ModifiedBy = userId;
            unit.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = units.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static DoseUnitDto MapToDto(DoseUnit u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Description = u.Description,
        IsActive = u.IsActive,
        DisplayOrder = u.DisplayOrder
    };
}
