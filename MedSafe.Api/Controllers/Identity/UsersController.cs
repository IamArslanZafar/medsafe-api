using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Infrastructure.Interfaces;
using MedSafe.Models;
using MedSafeAPI.DTOs;

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
        return Ok(users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            RoleId = u.RoleId,
            Unit = u.Unit,
            Title = u.Title,
            ProfessionId = u.ProfessionId,
            Status = u.Status,
            LastLogin = u.LastLogin,
            CreatedAt = u.CreatedAt,
            ProfileImage = u.ProfileImage
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
}
