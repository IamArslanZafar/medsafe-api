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
    // "Current Medications" field (mode="tags") and its Add Medication modal both
    // call this so a nurse/physician can add a medication inline while filling
    // out a report, same as an Admin adding one from Configurations.
    //
    // Find-or-create on the FULL (Name, DoseValue, DoseUnitId, RouteId,
    // FrequencyId, FormulationId) tuple, not on Name alone — two entries can
    // share a drug/strength label with different administration details (e.g.
    // "Warfarin 5mg" given IV at 8mg vs at 23mg are two distinct entries, both
    // worth keeping so each is selectable/auto-fillable on its own later). An
    // exact repeat of an existing tuple returns that row instead of duplicating
    // it. Update/Delete stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(CurrentMedicationUpsertDto dto)
    {
        var name = dto.Name.Trim();
        var existing = await _db.CurrentMedications.FirstOrDefaultAsync(m =>
            m.Name == name && m.DoseValue == dto.DoseValue && m.DoseUnitId == dto.DoseUnitId &&
            m.RouteId == dto.RouteId && m.FrequencyId == dto.FrequencyId && m.FormulationId == dto.FormulationId);
        if (existing != null)
            return Ok(MapToDto(existing));

        var nextOrder = dto.DisplayOrder
            ?? (await _db.CurrentMedications.AnyAsync() ? await _db.CurrentMedications.MaxAsync(m => m.DisplayOrder) + 1 : 1);

        var item = new CurrentMedication
        {
            Name = name,
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

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, CurrentMedicationUpsertDto dto)
    {
        var item = await _db.CurrentMedications.FindAsync(id);
        if (item == null) return NotFound();

        var name = dto.Name.Trim();
        if (await _db.CurrentMedications.AnyAsync(m =>
                m.Id != id && m.Name == name && m.DoseValue == dto.DoseValue && m.DoseUnitId == dto.DoseUnitId &&
                m.RouteId == dto.RouteId && m.FrequencyId == dto.FrequencyId && m.FormulationId == dto.FormulationId))
            return Conflict(new { message = "A medication with this exact name and dose/route/frequency/formulation already exists" });

        item.Name = name;
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
