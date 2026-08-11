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
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _repo;
    private readonly JwtService _jwt;
    private readonly IFileService _fileService;
    private readonly AppDbContext _db;

    public AuthController(IUserRepository repo, JwtService jwt, IFileService fileService, AppDbContext db)
    {
        _repo = repo;
        _jwt = jwt;
        _fileService = fileService;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _repo.GetByEmailAsync(dto.Email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });

        if (user.Status == "inactive") return Unauthorized(new { message = "Account is inactive" });

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            return Unauthorized(new { message = "Account locked. Try again later." });

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= 5)
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            await _repo.SaveAsync();
            return Unauthorized(new { message = "Invalid credentials" });
        }

        user.FailedAttempts = 0;
        user.LockedUntil = null;
        user.LastLogin = DateTime.UtcNow;

        var token = _jwt.GenerateToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();

        await _repo.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.GetRefreshTokenExpiryDays())
        });

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserId = user.Id,
            UserName = user.Name,
            Action = "LOGIN",
            Details = "User logged in",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();

        return Ok(new AuthResponseDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            Role = user.Role,
            Name = user.Name,
            Email = user.Email,
            Unit = user.Unit,
            Title = user.Title,
            ProfessionId = user.ProfessionId
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenDto dto)
    {
        var stored = await _repo.GetRefreshTokenAsync(dto.RefreshToken);

        if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        stored.IsRevoked = true;

        var newToken = _jwt.GenerateToken(stored.User);
        var newRefresh = _jwt.GenerateRefreshToken();

        await _repo.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = stored.UserId,
            Token = newRefresh,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.GetRefreshTokenExpiryDays())
        });

        await _repo.SaveAsync();

        return Ok(new RefreshResponseDto
        {
            AccessToken = newToken,
            RefreshToken = newRefresh
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshTokenDto dto)
    {
        await _repo.RevokeRefreshTokenAsync(dto.RefreshToken);
        await _repo.SaveAsync();
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromForm] RegisterDto dto)
    {
        if (await _repo.EmailExistsAsync(dto.Email))
            return Conflict(new { message = "Email already exists" });

        if (dto.ProfessionId.HasValue &&
            !await _db.Professions.AnyAsync(p => p.Id == dto.ProfessionId && p.IsActive))
            return BadRequest(new { message = "Invalid profession" });

        string? profileImagePath = null;
        if (dto.ProfileImage != null)
            profileImagePath = await _fileService.SaveProfileImageAsync(dto.ProfileImage);

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            Unit = dto.Unit,
            Title = dto.Title,
            ProfessionId = dto.ProfessionId,
            ProfileImage = profileImagePath
        };

        await _repo.AddAsync(user);

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)!.Value,
            Action = "REGISTER_USER",
            Details = $"Registered new user: {dto.Email} with role {dto.Role}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();
        return Ok(new { message = $"{dto.Role} account created.", userId = user.Id, profileImage = profileImagePath });
    }
}
