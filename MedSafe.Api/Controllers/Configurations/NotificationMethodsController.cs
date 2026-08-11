using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;
using MedSafe.Models;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/notification-methods")]
[Authorize]
public class NotificationMethodsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationMethodsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.NotificationMethods.AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);

        var items = await query.OrderBy(x => x.DisplayOrder).ToListAsync();
        return Ok(items.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.NotificationMethods.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(NotificationMethodUpsertDto dto)
    {
        if (await _db.NotificationMethods.AnyAsync(x => x.Code == dto.Code || x.Name == dto.Name))
            return Conflict(new { message = "A notification method with this code or name already exists" });

        var item = new NotificationMethod
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedBy = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.NotificationMethods.Add(item);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, NotificationMethodUpsertDto dto)
    {
        var item = await _db.NotificationMethods.FindAsync(id);
        if (item == null) return NotFound();

        if (await _db.NotificationMethods.AnyAsync(x => x.Id != id && (x.Code == dto.Code || x.Name == dto.Name)))
            return Conflict(new { message = "A notification method with this code or name already exists" });

        item.Code = dto.Code.Trim();
        item.Name = dto.Name.Trim();
        item.Description = dto.Description;
        item.IsActive = dto.IsActive;
        item.DisplayOrder = dto.DisplayOrder;
        item.ModifiedBy = _currentUser.UserId;
        item.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.NotificationMethods.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        item.ModifiedBy = _currentUser.UserId;
        item.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Notification method deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(NotificationMethodBulkDeleteDto dto)
    {
        var items = await _db.NotificationMethods.Where(x => dto.Ids.Contains(x.Id)).ToListAsync();

        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.IsActive = false;
            item.ModifiedBy = userId;
            item.ModifiedAt = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = items.Count });
    }

    private static NotificationMethodDto MapToDto(NotificationMethod x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        IsActive = x.IsActive,
        DisplayOrder = x.DisplayOrder
    };
}
