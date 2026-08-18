using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/process-stages")]
[Authorize]
public class StageOfProcessController : ControllerBase
{
    private readonly AppDbContext _db;

    public StageOfProcessController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.StageOfProcesses.AsQueryable();
        if (!includeInactive) query = query.Where(s => s.IsActive);

        var stages = await query.OrderBy(s => s.DisplayOrder).ToListAsync();
        return Ok(stages.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var stage = await _db.StageOfProcesses.FindAsync(id);
        if (stage == null) return NotFound();
        return Ok(MapToDto(stage));
    }

    // Open to any logged-in role — the incident report form's "Stage of Process"
    // field lets a user add a new one inline while filling out a report.
    // Update/Delete stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(StageOfProcessUpsertDto dto)
    {
        if (await _db.StageOfProcesses.AnyAsync(s => s.Name == dto.Name))
            return Conflict(new { message = "A process stage with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.StageOfProcesses.AnyAsync() ? await _db.StageOfProcesses.MaxAsync(s => s.DisplayOrder) + 1 : 1);

        var stage = new StageOfProcess
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.StageOfProcesses.Add(stage);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(stage));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, StageOfProcessUpsertDto dto)
    {
        var stage = await _db.StageOfProcesses.FindAsync(id);
        if (stage == null) return NotFound();

        if (await _db.StageOfProcesses.AnyAsync(s => s.Id != id && s.Name == dto.Name))
            return Conflict(new { message = "A process stage with this name already exists" });

        stage.Name = dto.Name;
        stage.Description = dto.Description;
        stage.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) stage.DisplayOrder = dto.DisplayOrder.Value;
        stage.ModifiedBy = CurrentUserId();
        stage.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(stage));
    }

    // Soft delete — IncidentReports.StageOfProcessId may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var stage = await _db.StageOfProcesses.FindAsync(id);
        if (stage == null) return NotFound();

        stage.IsActive = false;
        stage.ModifiedBy = CurrentUserId();
        stage.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Process stage deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(StageOfProcessBulkDeleteDto dto)
    {
        var stages = await _db.StageOfProcesses
            .Where(s => dto.Ids.Contains(s.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var stage in stages)
        {
            stage.IsActive = false;
            stage.ModifiedBy = userId;
            stage.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = stages.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static StageOfProcessDto MapToDto(StageOfProcess s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        IsActive = s.IsActive,
        DisplayOrder = s.DisplayOrder
    };
}
