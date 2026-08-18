using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/error-categories")]
[Authorize]
public class ErrorCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ErrorCategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.ErrorCategories.AsQueryable();
        if (!includeInactive) query = query.Where(c => c.IsActive);

        var categories = await query.OrderBy(c => c.DisplayOrder).ToListAsync();
        return Ok(categories.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _db.ErrorCategories.FindAsync(id);
        if (category == null) return NotFound();
        return Ok(MapToDto(category));
    }

    // Open to any logged-in role — the incident report form's "Error Category"
    // field lets a user add a new one inline while filling out a report.
    // Update/Delete stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(ErrorCategoryUpsertDto dto)
    {
        if (await _db.ErrorCategories.AnyAsync(c => c.Name == dto.Name))
            return Conflict(new { message = "An error category with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.ErrorCategories.AnyAsync() ? await _db.ErrorCategories.MaxAsync(c => c.DisplayOrder) + 1 : 1);

        var category = new ErrorCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.ErrorCategories.Add(category);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(category));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ErrorCategoryUpsertDto dto)
    {
        var category = await _db.ErrorCategories.FindAsync(id);
        if (category == null) return NotFound();

        if (await _db.ErrorCategories.AnyAsync(c => c.Id != id && c.Name == dto.Name))
            return Conflict(new { message = "An error category with this name already exists" });

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) category.DisplayOrder = dto.DisplayOrder.Value;
        category.ModifiedBy = CurrentUserId();
        category.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(category));
    }

    // Soft delete — IncidentReports.ErrorCategoryId may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.ErrorCategories.FindAsync(id);
        if (category == null) return NotFound();

        category.IsActive = false;
        category.ModifiedBy = CurrentUserId();
        category.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Error category deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(ErrorCategoryBulkDeleteDto dto)
    {
        var categories = await _db.ErrorCategories
            .Where(c => dto.Ids.Contains(c.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var category in categories)
        {
            category.IsActive = false;
            category.ModifiedBy = userId;
            category.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = categories.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static ErrorCategoryDto MapToDto(ErrorCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        IsActive = c.IsActive,
        DisplayOrder = c.DisplayOrder
    };
}
