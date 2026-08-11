using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs()
    {
        var logs = await _db.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(500)
            .Select(l => new
            {
                l.Id,
                l.UserName,
                l.Action,
                l.Details,
                l.IpAddress,
                l.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }
}
