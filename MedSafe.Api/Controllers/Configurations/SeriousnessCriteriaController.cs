using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/seriousness-criteria")]
[Authorize]
public class SeriousnessCriteriaController : ControllerBase
{
    private readonly AppDbContext _db;

    public SeriousnessCriteriaController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.SeriousnessCriteria.AsQueryable();
        if (!includeInactive) query = query.Where(c => c.IsActive);

        var criteria = await query.OrderBy(c => c.DisplayOrder).ToListAsync();
        return Ok(criteria.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var criterion = await _db.SeriousnessCriteria.FindAsync(id);
        if (criterion == null) return NotFound();
        return Ok(MapToDto(criterion));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(SeriousnessCriterionUpsertDto dto)
    {
        if (await _db.SeriousnessCriteria.AnyAsync(c => c.Name == dto.Name))
            return Conflict(new { message = "A seriousness criterion with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.SeriousnessCriteria.AnyAsync() ? await _db.SeriousnessCriteria.MaxAsync(c => c.DisplayOrder) + 1 : 1);

        var criterion = new SeriousnessCriterion
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.SeriousnessCriteria.Add(criterion);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(criterion));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, SeriousnessCriterionUpsertDto dto)
    {
        var criterion = await _db.SeriousnessCriteria.FindAsync(id);
        if (criterion == null) return NotFound();

        if (await _db.SeriousnessCriteria.AnyAsync(c => c.Id != id && c.Name == dto.Name))
            return Conflict(new { message = "A seriousness criterion with this name already exists" });

        criterion.Name = dto.Name;
        criterion.Description = dto.Description;
        criterion.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) criterion.DisplayOrder = dto.DisplayOrder.Value;
        criterion.ModifiedBy = CurrentUserId();
        criterion.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(criterion));
    }

    // Soft delete — IncidentReportSeriousnessCriterion may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var criterion = await _db.SeriousnessCriteria.FindAsync(id);
        if (criterion == null) return NotFound();

        criterion.IsActive = false;
        criterion.ModifiedBy = CurrentUserId();
        criterion.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Seriousness criterion deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(SeriousnessCriterionBulkDeleteDto dto)
    {
        var criteria = await _db.SeriousnessCriteria
            .Where(c => dto.Ids.Contains(c.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var criterion in criteria)
        {
            criterion.IsActive = false;
            criterion.ModifiedBy = userId;
            criterion.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = criteria.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static SeriousnessCriterionDto MapToDto(SeriousnessCriterion c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        IsActive = c.IsActive,
        DisplayOrder = c.DisplayOrder
    };
}
