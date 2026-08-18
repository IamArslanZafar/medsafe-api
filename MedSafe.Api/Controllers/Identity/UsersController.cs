using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Infrastructure.Interfaces;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repo;
    private readonly AppDbContext _db;

    public UsersController(IUserRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _repo.GetAllAsync();
        var professionNames = await _db.Professions.ToDictionaryAsync(p => p.Id, p => p.Name);
        var positionNames = await _db.Positions.ToDictionaryAsync(p => p.Id, p => p.Name);
        var availabilityByUser = (await _db.UserAvailabilities.AsNoTracking().ToListAsync())
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.DayOfWeek).Select(AvailabilityHelper.ToDto).ToList());

        return Ok(users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            RoleId = u.RoleId,
            Unit = u.Unit,
            Title = u.Title,
            PhoneNumber = u.PhoneNumber,
            Shift = u.Shift,
            ProfessionId = u.ProfessionId,
            ProfessionName = u.ProfessionId.HasValue ? professionNames.GetValueOrDefault(u.ProfessionId.Value) : null,
            PositionId = u.PositionId,
            PositionName = u.PositionId.HasValue ? positionNames.GetValueOrDefault(u.PositionId.Value) : null,
            Status = u.Status,
            LastLogin = u.LastLogin,
            CreatedAt = u.CreatedAt,
            ProfileImage = u.ProfileImage,
            Availability = availabilityByUser.GetValueOrDefault(u.Id, [])
        }));
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateUserStatusDto dto)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.Status = dto.Status;

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserName = User.FindFirst(ClaimTypes.Name)!.Value,
            Action = "UPDATE_USER_STATUS",
            Details = $"User {user.Email} status changed to {dto.Status}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();
        return Ok(new { message = $"User status updated to {dto.Status}" });
    }

    // Keeps User.Role (string) in sync with User.RoleId — the string is still what
    // [Authorize(Roles = "...")] checks everywhere, so a Role reassignment has to
    // update both or the user's actual backend access wouldn't match the Role they
    // now show as having.
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(int id, UpdateUserRoleDto dto)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null) return NotFound();

        var role = await _db.Roles.FindAsync(dto.RoleId);
        if (role == null) return BadRequest(new { message = "Invalid role" });

        var previousRole = user.Role;
        user.RoleId = role.Id;
        user.Role = role.Name;

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserName = User.FindFirst(ClaimTypes.Name)!.Value,
            Action = "UPDATE_USER_ROLE",
            Details = $"User {user.Email} role changed from {previousRole} to {role.Name}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();
        return Ok(new { message = $"Role updated to {role.Name}" });
    }

    [HttpPut("{id}/profession")]
    public async Task<IActionResult> UpdateProfession(int id, UpdateUserProfessionDto dto)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null) return NotFound();

        if (dto.ProfessionId.HasValue &&
            !await _db.Professions.AnyAsync(p => p.Id == dto.ProfessionId && p.IsActive))
            return BadRequest(new { message = "Invalid profession" });

        if (dto.PositionId.HasValue &&
            !await _db.Positions.AnyAsync(p => p.Id == dto.PositionId && p.ProfessionId == dto.ProfessionId && p.IsActive))
            return BadRequest(new { message = "Invalid position for the selected profession" });

        user.ProfessionId = dto.ProfessionId;
        user.PositionId = dto.PositionId;
        user.PhoneNumber = dto.PhoneNumber;

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserName = User.FindFirst(ClaimTypes.Name)!.Value,
            Action = "UPDATE_USER_PROFESSION",
            Details = $"User {user.Email} profession/position updated (ProfessionId={dto.ProfessionId}, PositionId={dto.PositionId}, PhoneNumber={dto.PhoneNumber})",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();
        return Ok(new { message = "Profession/position updated" });
    }

    [HttpGet("{id}/availability")]
    public async Task<IActionResult> GetAvailability(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null) return NotFound();

        var rows = await _db.UserAvailabilities
            .AsNoTracking()
            .Where(a => a.UserId == id)
            .OrderBy(a => a.DayOfWeek)
            .ToListAsync();

        return Ok(rows.Select(AvailabilityHelper.ToDto));
    }

    // Replace-all semantics — the frontend always resends the complete current
    // weekly schedule (unchecked days are simply omitted), so the old set of
    // rows is fully replaced rather than diffed.
    [HttpPut("{id}/availability")]
    public async Task<IActionResult> UpdateAvailability(int id, UpdateUserAvailabilityDto dto)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null) return NotFound();

        var error = AvailabilityHelper.Validate(dto.Availability);
        if (error != null) return BadRequest(new { message = error });

        var existing = await _db.UserAvailabilities.Where(a => a.UserId == id).ToListAsync();
        _db.UserAvailabilities.RemoveRange(existing);
        _db.UserAvailabilities.AddRange(AvailabilityHelper.BuildEntities(id, dto.Availability));

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserName = User.FindFirst(ClaimTypes.Name)!.Value,
            Action = "UPDATE_USER_AVAILABILITY",
            Details = $"User {user.Email} weekly availability updated ({dto.Availability.Count} day(s)).",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();
        return Ok(new { message = "User availability updated successfully.", availabilityDays = dto.Availability.Count });
    }
}
