using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RolesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _db.Roles.AsNoTracking()
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                PermissionCount = r.RolePermissions.Count(),
                UserCount = _db.Users.Count(u => u.RoleId == r.Id)
            })
            .OrderBy(r => r.Name)
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();

        var permissionIds = await _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        return Ok(new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            PermissionCount = permissionIds.Count,
            UserCount = await _db.Users.CountAsync(u => u.RoleId == id),
            PermissionIds = permissionIds
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(RoleUpsertDto dto)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == dto.Name))
            return Conflict(new { message = "A role with this name already exists" });

        var validPermissionIds = await ValidatePermissionIdsAsync(dto.PermissionIds);
        if (validPermissionIds == null)
            return BadRequest(new { message = "One or more permission IDs are invalid" });

        var role = new Role
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        foreach (var permissionId in validPermissionIds)
            _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        await _db.SaveChangesAsync();

        return await GetById(role.Id);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, RoleUpsertDto dto)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();

        if (await _db.Roles.AnyAsync(r => r.Id != id && r.Name == dto.Name))
            return Conflict(new { message = "A role with this name already exists" });

        var validPermissionIds = await ValidatePermissionIdsAsync(dto.PermissionIds);
        if (validPermissionIds == null)
            return BadRequest(new { message = "One or more permission IDs are invalid" });

        role.Name = dto.Name;
        role.Description = dto.Description;

        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);
        foreach (var permissionId in validPermissionIds)
            _db.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = permissionId });

        await _db.SaveChangesAsync();
        return await GetById(id);
    }

    // Blocks deletion if any user is still assigned this role — same guard as
    // SystemMonitorControl's RoleController, prevents orphaning a user's RoleId.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();

        if (await _db.Users.AnyAsync(u => u.RoleId == id))
            return Conflict(new { message = "Cannot delete a role that is still assigned to users" });

        var rolePermissions = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
        _db.RolePermissions.RemoveRange(rolePermissions);
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Role deleted" });
    }

    private async Task<List<int>?> ValidatePermissionIdsAsync(List<int> permissionIds)
    {
        var distinct = permissionIds.Distinct().ToList();
        var validCount = await _db.Permissions.CountAsync(p => distinct.Contains(p.Id));
        return validCount == distinct.Count ? distinct : null;
    }
}
