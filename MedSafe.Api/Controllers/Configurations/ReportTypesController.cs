using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

// Read-only — only Medication Error + ADR are valid report types, so Admin
// cannot create/edit/delete rows here (would break report validation/alerts).
[ApiController]
[Route("api/report-types")]
[Authorize]
public class ReportTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportTypesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.ReportTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.ReportTypes.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(MapToDto(item));
    }

    private static ReportTypeDto MapToDto(ReportType x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        IsActive = x.IsActive,
        DisplayOrder = x.DisplayOrder
    };
}
