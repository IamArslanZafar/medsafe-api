using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

// Read-only — WHO-UMC causality scale is a fixed clinical standard, no Admin CRUD.
[ApiController]
[Route("api/suspected-causalities")]
[Authorize]
public class SuspectedCausalitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SuspectedCausalitiesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.SuspectedCausalities
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.SuspectedCausalities.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    private static SuspectedCausalityDto MapToDto(SuspectedCausality x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        IsActive = x.IsActive,
        DisplayOrder = x.DisplayOrder
    };
}
