using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public UsersController(IUserRepository repo) => _repo = repo;

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
}
