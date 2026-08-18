using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/routes")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoutesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.Routes.AsQueryable();
        if (!includeInactive) query = query.Where(r => r.IsActive);

        var routes = await query.OrderBy(r => r.DisplayOrder).ToListAsync();
        return Ok(routes.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var route = await _db.Routes.FindAsync(id);
        if (route == null) return NotFound();
        return Ok(MapToDto(route));
    }

    // Open to any logged-in role — the incident report form's medication fields
    // let a user add a new Route inline while filling out a report. Update/Delete
    // stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(RouteUpsertDto dto)
    {
        if (await _db.Routes.AnyAsync(r => r.Name == dto.Name))
            return Conflict(new { message = "A route with this name already exists" });

        var nextOrder = dto.DisplayOrder
            ?? (await _db.Routes.AnyAsync() ? await _db.Routes.MaxAsync(r => r.DisplayOrder) + 1 : 1);

        var route = new MedSafe.Models.Route
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId() ?? 0,
            CreatedDate = DateTime.UtcNow
        };

        _db.Routes.Add(route);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(route));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, RouteUpsertDto dto)
    {
        var route = await _db.Routes.FindAsync(id);
        if (route == null) return NotFound();

        if (await _db.Routes.AnyAsync(r => r.Id != id && r.Name == dto.Name))
            return Conflict(new { message = "A route with this name already exists" });

        route.Name = dto.Name;
        route.Description = dto.Description;
        route.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) route.DisplayOrder = dto.DisplayOrder.Value;
        route.ModifiedBy = CurrentUserId();
        route.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(route));
    }

    // Soft delete — IncidentReports.RouteId may already reference this row.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var route = await _db.Routes.FindAsync(id);
        if (route == null) return NotFound();

        route.IsActive = false;
        route.ModifiedBy = CurrentUserId();
        route.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Route deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(RouteBulkDeleteDto dto)
    {
        var routes = await _db.Routes
            .Where(r => dto.Ids.Contains(r.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var route in routes)
        {
            route.IsActive = false;
            route.ModifiedBy = userId;
            route.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = routes.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static RouteDto MapToDto(MedSafe.Models.Route r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        IsActive = r.IsActive,
        DisplayOrder = r.DisplayOrder
    };
}
