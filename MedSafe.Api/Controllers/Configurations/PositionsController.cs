using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/positions")]
[Authorize]
public class PositionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PositionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? professionId = null, [FromQuery] bool includeInactive = false)
    {
        var query = _db.Positions.AsQueryable();
        if (professionId.HasValue) query = query.Where(p => p.ProfessionId == professionId.Value);
        if (!includeInactive) query = query.Where(p => p.IsActive);

        var positions = await query.OrderBy(p => p.ProfessionId).ThenBy(p => p.DisplayOrder).ToListAsync();
        return Ok(positions.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var position = await _db.Positions.FindAsync(id);
        if (position == null) return NotFound();
        return Ok(MapToDto(position));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(PositionUpsertDto dto)
    {
        if (!await _db.Professions.AnyAsync(p => p.Id == dto.ProfessionId && p.IsActive))
            return BadRequest(new { message = "Invalid profession" });

        if (await _db.Positions.AnyAsync(p => p.ProfessionId == dto.ProfessionId && p.Name == dto.Name))
            return Conflict(new { message = "A position with this name already exists for this profession" });

        var nextOrder = dto.DisplayOrder;
        if (nextOrder is null)
        {
            var maxOrder = await _db.Positions
                .Where(p => p.ProfessionId == dto.ProfessionId)
                .MaxAsync(p => (int?)p.DisplayOrder);
            nextOrder = (maxOrder ?? 0) + 1;
        }

        var position = new Position
        {
            ProfessionId = dto.ProfessionId,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId(),
            CreatedDate = DateTime.UtcNow
        };

        _db.Positions.Add(position);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(position));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, PositionUpsertDto dto)
    {
        var position = await _db.Positions.FindAsync(id);
        if (position == null) return NotFound();

        if (!await _db.Professions.AnyAsync(p => p.Id == dto.ProfessionId && p.IsActive))
            return BadRequest(new { message = "Invalid profession" });

        if (await _db.Positions.AnyAsync(p => p.Id != id && p.ProfessionId == dto.ProfessionId && p.Name == dto.Name))
            return Conflict(new { message = "A position with this name already exists for this profession" });

        position.ProfessionId = dto.ProfessionId;
        position.Name = dto.Name;
        position.Description = dto.Description;
        position.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) position.DisplayOrder = dto.DisplayOrder.Value;
        position.ModifiedBy = CurrentUserId();
        position.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(position));
    }

    // Soft delete — Users may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var position = await _db.Positions.FindAsync(id);
        if (position == null) return NotFound();

        position.IsActive = false;
        position.ModifiedBy = CurrentUserId();
        position.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Position deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(PositionBulkDeleteDto dto)
    {
        var positions = await _db.Positions
            .Where(p => dto.Ids.Contains(p.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var position in positions)
        {
            position.IsActive = false;
            position.ModifiedBy = userId;
            position.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = positions.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static PositionDto MapToDto(Position p) => new()
    {
        Id = p.Id,
        ProfessionId = p.ProfessionId,
        Name = p.Name,
        Description = p.Description,
        IsActive = p.IsActive,
        DisplayOrder = p.DisplayOrder
    };
}
