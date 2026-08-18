using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/patient-outcomes")]
[Authorize]
public class PatientOutcomesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientOutcomesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.PatientOutcomes.AsQueryable();
        if (!includeInactive) query = query.Where(o => o.IsActive);

        var outcomes = await query.OrderBy(o => o.DisplayOrder).ToListAsync();
        return Ok(outcomes.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var outcome = await _db.PatientOutcomes.FindAsync(id);
        if (outcome == null) return NotFound();
        return Ok(MapToDto(outcome));
    }

    // Open to any logged-in role — the incident report form's "Patient Outcome"
    // field lets a user add a new one inline while filling out a report.
    // Update/Delete stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(PatientOutcomeUpsertDto dto)
    {
        if (await _db.PatientOutcomes.AnyAsync(o => o.Name == dto.Name))
            return Conflict(new { message = "A patient outcome with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.PatientOutcomes.AnyAsync() ? await _db.PatientOutcomes.MaxAsync(o => o.DisplayOrder) + 1 : 1);

        var outcome = new PatientOutcome
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.PatientOutcomes.Add(outcome);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(outcome));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, PatientOutcomeUpsertDto dto)
    {
        var outcome = await _db.PatientOutcomes.FindAsync(id);
        if (outcome == null) return NotFound();

        if (await _db.PatientOutcomes.AnyAsync(o => o.Id != id && o.Name == dto.Name))
            return Conflict(new { message = "A patient outcome with this name already exists" });

        outcome.Name = dto.Name;
        outcome.Description = dto.Description;
        outcome.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) outcome.DisplayOrder = dto.DisplayOrder.Value;
        outcome.ModifiedBy = CurrentUserId();
        outcome.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(outcome));
    }

    // Soft delete — IncidentReports.PatientOutcomeId may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var outcome = await _db.PatientOutcomes.FindAsync(id);
        if (outcome == null) return NotFound();

        outcome.IsActive = false;
        outcome.ModifiedBy = CurrentUserId();
        outcome.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Patient outcome deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(PatientOutcomeBulkDeleteDto dto)
    {
        var outcomes = await _db.PatientOutcomes
            .Where(o => dto.Ids.Contains(o.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var outcome in outcomes)
        {
            outcome.IsActive = false;
            outcome.ModifiedBy = userId;
            outcome.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = outcomes.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static PatientOutcomeDto MapToDto(PatientOutcome o) => new()
    {
        Id = o.Id,
        Name = o.Name,
        Description = o.Description,
        IsActive = o.IsActive,
        DisplayOrder = o.DisplayOrder
    };
}
