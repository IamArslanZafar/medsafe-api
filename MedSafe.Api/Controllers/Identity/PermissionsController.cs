using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PermissionsController(AppDbContext db) => _db = db;

    // GET /api/permissions/tree — module-grouped, recursive tree matching the
    // "Assign Role Permissions" modal (tabs per module, nested checkbox tree per tab).
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var modules = await _db.SystemModules.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();
        var permissions = await _db.Permissions.AsNoTracking().ToListAsync();
        var byParent = permissions.ToLookup(p => p.ParentId);

        PermissionNodeDto BuildNode(MedSafe.Models.Permission p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            PermissionTag = p.PermissionTag,
            ParentId = p.ParentId,
            Children = byParent[p.Id].OrderBy(c => c.Id).Select(BuildNode).ToList()
        };

        var result = modules.Select(m => new PermissionModuleDto
        {
            ModuleId = m.Id,
            ModuleName = m.Name,
            Permissions = byParent[null]
                .Where(p => p.SystemModuleId == m.Id)
                .OrderBy(p => p.Id)
                .Select(BuildNode)
                .ToList()
        }).ToList();

        return Ok(result);
    }
}
