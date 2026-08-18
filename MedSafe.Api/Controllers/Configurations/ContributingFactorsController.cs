using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;
using MedSafe.Models;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/contributing-factors")]
[Authorize]
public class ContributingFactorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContributingFactorsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.ContributingFactors.AsQueryable();
        if (!includeInactive) query = query.Where(f => f.IsActive);

        var factors = await query.OrderBy(f => f.DisplayOrder).ToListAsync();
        return Ok(factors.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var factor = await _db.ContributingFactors.FindAsync(id);
        if (factor == null) return NotFound();
        return Ok(MapToDto(factor));
    }

    // Open to any logged-in role — the incident report form's "Contributing
    // Factors" field lets a user add a new one inline while filling out a
    // report. Update/Delete stay Admin-only.
    [HttpPost]
    public async Task<IActionResult> Create(ContributingFactorUpsertDto dto)
    {
        var code = await GenerateCodeAsync(dto.Name);
        var nextOrder = dto.DisplayOrder
            ?? (await _db.ContributingFactors.AnyAsync() ? await _db.ContributingFactors.MaxAsync(f => f.DisplayOrder) + 1 : 1);

        var factor = new ContributingFactor
        {
            Code = code,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = nextOrder,
            CreatedBy = CurrentUserId(),
            CreatedDate = DateTime.UtcNow
        };

        _db.ContributingFactors.Add(factor);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(factor));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ContributingFactorUpsertDto dto)
    {
        var factor = await _db.ContributingFactors.FindAsync(id);
        if (factor == null) return NotFound();

        factor.Name = dto.Name;
        factor.Description = dto.Description;
        factor.IsActive = dto.IsActive;
        if (dto.DisplayOrder.HasValue) factor.DisplayOrder = dto.DisplayOrder.Value;
        factor.ModifiedBy = CurrentUserId();
        factor.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(factor));
    }

    // Soft delete — IncidentReportContributingFactor may already reference this row,
    // so it's deactivated (IsActive = false) rather than removed.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var factor = await _db.ContributingFactors.FindAsync(id);
        if (factor == null) return NotFound();

        factor.IsActive = false;
        factor.ModifiedBy = CurrentUserId();
        factor.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Contributing factor deactivated" });
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(ContributingFactorBulkDeleteDto dto)
    {
        var factors = await _db.ContributingFactors
            .Where(f => dto.Ids.Contains(f.Id))
            .ToListAsync();

        var userId = CurrentUserId();
        var now = DateTime.UtcNow;
        foreach (var factor in factors)
        {
            factor.IsActive = false;
            factor.ModifiedBy = userId;
            factor.ModifiedDate = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { deactivated = factors.Count });
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private async Task<string> GenerateCodeAsync(string name)
    {
        var raw = name.ToUpperInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var code = new string(raw);
        while (code.Contains("__")) code = code.Replace("__", "_");
        code = code.Trim('_');
        if (string.IsNullOrEmpty(code)) code = "FACTOR";

        var baseCode = code;
        var suffix = 2;
        while (await _db.ContributingFactors.AnyAsync(f => f.Code == code))
        {
            code = $"{baseCode}_{suffix}";
            suffix++;
        }
        return code;
    }

    private static ContributingFactorDto MapToDto(ContributingFactor f) => new()
    {
        Id = f.Id,
        Code = f.Code,
        Name = f.Name,
        Description = f.Description,
        IsActive = f.IsActive,
        DisplayOrder = f.DisplayOrder
    };
}
