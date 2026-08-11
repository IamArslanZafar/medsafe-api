using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;
using MedSafe.Models;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/allergies")]
[Authorize]
public class AllergiesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AllergiesController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.Allergies.AsQueryable();
        if (!includeInactive) query = query.Where(a => a.IsActive);

        var items = await query.OrderBy(a => a.DisplayOrder).ToListAsync();
        return Ok(items.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Allergies.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(AllergyUpsertDto dto)
    {
        if (await _db.Allergies.AnyAsync(a => a.Name == dto.Name))
            return Conflict(new { message = "An allergy with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.Allergies.AnyAsync() ? await _db.Allergies.MaxAsync(a => a.DisplayOrder) + 1 : 1);

        var item = new Allergy
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow
        };

        _db.Allergies.Add(item);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, AllergyUpsertDto dto)
    {
        var item = await _db.Allergies.FindAsync(id);
        if (item == null) return NotFound();

        if (await _db.Allergies.AnyAsync(a => a.Id != id && a.Name == dto.Name))
            return Conflict(new { message = "An allergy with this name already exists" });

        item.Name = dto.Name.Trim();
        item.Description = dto.Description;
        item.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) item.DisplayOrder = dto.DisplayOrder.Value;
        item.ModifiedBy = _currentUser.UserId;
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Allergies.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        item.ModifiedBy = _currentUser.UserId;
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Allergy deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(AllergyBulkDeleteDto dto)
    {
        var items = await _db.Allergies.Where(a => dto.Ids.Contains(a.Id)).ToListAsync();

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

    private static AllergyDto MapToDto(Allergy a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Description = a.Description,
        IsActive = a.IsActive,
        DisplayOrder = a.DisplayOrder
    };
}
