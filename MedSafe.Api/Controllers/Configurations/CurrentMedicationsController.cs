using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;
using MedSafe.Models;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/current-medications")]
[Authorize]
public class CurrentMedicationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CurrentMedicationsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.CurrentMedications.AsQueryable();
        if (!includeInactive) query = query.Where(m => m.IsActive);

        var items = await query.OrderBy(m => m.DisplayOrder).ToListAsync();
        return Ok(items.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.CurrentMedications.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    // Open to any logged-in role (not just Admin) — the incident report form's
    // "Current Medications" field (mode="tags") calls this so a nurse/physician
    // can add a new medication inline while filling out a report, same as an
    // Admin adding one from Configurations. Update/Delete stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(CurrentMedicationUpsertDto dto)
    {
        if (await _db.CurrentMedications.AnyAsync(m => m.Name == dto.Name))
            return Conflict(new { message = "A medication with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.CurrentMedications.AnyAsync() ? await _db.CurrentMedications.MaxAsync(m => m.DisplayOrder) + 1 : 1);

        var item = new CurrentMedication
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            DoseValue = dto.DoseValue,
            DoseUnitId = dto.DoseUnitId,
            RouteId = dto.RouteId,
            FrequencyId = dto.FrequencyId,
            FormulationId = dto.FormulationId,
            CreatedBy = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow
        };

        _db.CurrentMedications.Add(item);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    // Any logged-in role — called once, right after a brand-new medication is
    // created inline from the Incident Report wizard's Add Medication modal, to
    // record its first-ever Dose/Unit/Route/Frequency/Formulation so picking it
    // again on a later report can auto-fill those fields. Only fills fields that
    // are still null; never overwrites a value the entry already has, so a later
    // report using different values for the same drug can't silently rewrite the
    // shared lookup. Update (below) is a deliberate Admin correction instead, and
    // always overwrites with whatever the Configurations edit form has.
    [HttpPut("{id}/dose-defaults")]
    public async Task<IActionResult> SetDoseDefaults(int id, SetCurrentMedicationDoseDefaultsDto dto)
    {
        var item = await _db.CurrentMedications.FindAsync(id);
        if (item == null) return NotFound();

        item.DoseValue ??= dto.DoseValue;
        item.DoseUnitId ??= dto.DoseUnitId;
        item.RouteId ??= dto.RouteId;
        item.FrequencyId ??= dto.FrequencyId;
        item.FormulationId ??= dto.FormulationId;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, CurrentMedicationUpsertDto dto)
    {
        var item = await _db.CurrentMedications.FindAsync(id);
        if (item == null) return NotFound();

        if (await _db.CurrentMedications.AnyAsync(m => m.Id != id && m.Name == dto.Name))
            return Conflict(new { message = "A medication with this name already exists" });

        item.Name = dto.Name.Trim();
        item.Description = dto.Description;
        item.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) item.DisplayOrder = dto.DisplayOrder.Value;
        item.DoseValue = dto.DoseValue;
        item.DoseUnitId = dto.DoseUnitId;
        item.RouteId = dto.RouteId;
        item.FrequencyId = dto.FrequencyId;
        item.FormulationId = dto.FormulationId;
        item.ModifiedBy = _currentUser.UserId;
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.CurrentMedications.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        item.ModifiedBy = _currentUser.UserId;
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Medication deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(CurrentMedicationBulkDeleteDto dto)
    {
        var items = await _db.CurrentMedications.Where(m => dto.Ids.Contains(m.Id)).ToListAsync();

        var userId = _currentUser.UserId;
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

    private static CurrentMedicationDto MapToDto(CurrentMedication m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Description = m.Description,
        IsActive = m.IsActive,
        DisplayOrder = m.DisplayOrder,
        DoseValue = m.DoseValue,
        DoseUnitId = m.DoseUnitId,
        RouteId = m.RouteId,
        FrequencyId = m.FrequencyId,
        FormulationId = m.FormulationId
    };
}
