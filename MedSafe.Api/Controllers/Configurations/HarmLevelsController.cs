using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

// Read-only — NCC MERP A-I is a fixed clinical standard, no Admin CRUD.
[ApiController]
[Route("api/harm-levels")]
[Authorize]
public class HarmLevelsController : ControllerBase
{
    private readonly AppDbContext _db;

    public HarmLevelsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.HarmLevels
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.HarmLevels.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    private static HarmLevelDto MapToDto(HarmLevel x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        SeverityRank = x.SeverityRank,
        GroupName = x.GroupName,
        IsActive = x.IsActive,
        DisplayOrder = x.DisplayOrder
    };
}
