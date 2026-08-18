using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/formulations")]
[Authorize]
public class FormulationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FormulationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.Formulations.AsQueryable();
        if (!includeInactive) query = query.Where(f => f.IsActive);

        var formulations = await query.OrderBy(f => f.DisplayOrder).ToListAsync();
        return Ok(formulations.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var formulation = await _db.Formulations.FindAsync(id);
        if (formulation == null) return NotFound();
        return Ok(MapToDto(formulation));
    }

    // Open to any logged-in role — the incident report form's medication fields
    // let a user add a new Formulation inline while filling out a report.
    // Update/Delete stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(FormulationUpsertDto dto)
    {
        if (await _db.Formulations.AnyAsync(f => f.Name == dto.Name))
            return Conflict(new { message = "A formulation with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.Formulations.AnyAsync() ? await _db.Formulations.MaxAsync(f => f.DisplayOrder) + 1 : 1);

        var formulation = new Formulation
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.Formulations.Add(formulation);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(formulation));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, FormulationUpsertDto dto)
    {
        var formulation = await _db.Formulations.FindAsync(id);
        if (formulation == null) return NotFound();

        if (await _db.Formulations.AnyAsync(f => f.Id != id && f.Name == dto.Name))
            return Conflict(new { message = "A formulation with this name already exists" });

        formulation.Name = dto.Name;
        formulation.Description = dto.Description;
        formulation.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) formulation.DisplayOrder = dto.DisplayOrder.Value;
        formulation.ModifiedBy = CurrentUserId();
        formulation.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(formulation));
    }

    // Soft delete — IncidentReports.FormulationId may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var formulation = await _db.Formulations.FindAsync(id);
        if (formulation == null) return NotFound();

        formulation.IsActive = false;
        formulation.ModifiedBy = CurrentUserId();
        formulation.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Formulation deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(FormulationBulkDeleteDto dto)
    {
        var formulations = await _db.Formulations
            .Where(f => dto.Ids.Contains(f.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var formulation in formulations)
        {
            formulation.IsActive = false;
            formulation.ModifiedBy = userId;
            formulation.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = formulations.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static FormulationDto MapToDto(Formulation f) => new()
    {
        Id = f.Id,
        Name = f.Name,
        Description = f.Description,
        IsActive = f.IsActive,
        DisplayOrder = f.DisplayOrder
    };
}
