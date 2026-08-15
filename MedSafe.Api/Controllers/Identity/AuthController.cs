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
    private readonly ICurrentUserService _currentUser;

    public AuthController(IUserRepository repo, JwtService jwt, IFileService fileService, AppDbContext db, ICurrentUserService currentUser)
    {
        _repo = repo;
        _jwt = jwt;
        _fileService = fileService;
        _db = db;
        _currentUser = currentUser;
    }

    private async Task<List<string>> GetPermissionTagsAsync(int? roleId) =>
        roleId.HasValue
            ? await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.PermissionTag)
                .ToListAsync()
            : new List<string>();

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

        var permissions = await GetPermissionTagsAsync(user.RoleId);

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
            ProfessionId = user.ProfessionId,
            ProfileImage = user.ProfileImage,
            Permissions = permissions
        });
    }

    // Lets the frontend pull a fresh permission set for the logged-in user without a
    // full re-login — needed because an Admin editing a Role's permissions (Roles &
    // Permissions screen) doesn't retroactively touch tokens already issued to users
    // holding that Role. Mirrors D:\SystemMonitorControl's GET /api/Permission/getUserPermissions/{UserId},
    // scoped to "my own permissions" instead of an arbitrary user id.
    [HttpGet("permissions")]
    [Authorize]
    public async Task<IActionResult> GetMyPermissions()
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == _currentUser.UserId);
        if (user == null) return NotFound();

        var permissions = await GetPermissionTagsAsync(user.RoleId);
        return Ok(new { permissions });
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

    // Self-service — always keyed off the JWT's own user id, never a client-supplied
    // one. Name isn't a login credential, so unlike email/password below it doesn't
    // require re-entering the current password.
    [HttpPut("change-name")]
    [Authorize]
    public async Task<IActionResult> ChangeName(ChangeNameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewName))
            return BadRequest(new { message = "Name cannot be empty." });

        var user = await _repo.GetByIdAsync(_currentUser.UserId);
        if (user == null) return NotFound();

        user.Name = dto.NewName.Trim();

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserId = user.Id,
            UserName = user.Name,
            Action = "CHANGE_NAME",
            Details = "User changed their own display name",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();
        return Ok(new { message = "Name updated successfully.", name = user.Name });
    }

    // Re-verifies the current password so a stolen/left-open session alone isn't
    // enough to hijack the account's login credentials.
    [HttpPut("change-email")]
    [Authorize]
    public async Task<IActionResult> ChangeEmail(ChangeEmailDto dto)
    {
        var user = await _repo.GetByIdAsync(_currentUser.UserId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        if (!string.Equals(dto.NewEmail, user.Email, StringComparison.OrdinalIgnoreCase) &&
            await _repo.EmailExistsAsync(dto.NewEmail))
            return Conflict(new { message = "Email already in use." });

        user.Email = dto.NewEmail;

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserId = user.Id,
            UserName = user.Name,
            Action = "CHANGE_EMAIL",
            Details = "User changed their own email address",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();
        return Ok(new { message = "Email updated successfully.", email = user.Email });
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await _repo.GetByIdAsync(_currentUser.UserId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await _repo.AddAuditLogAsync(new AuditLog
        {
            UserId = user.Id,
            UserName = user.Name,
            Action = "CHANGE_PASSWORD",
            Details = "User changed their own password",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _repo.SaveAsync();
        return Ok(new { message = "Password updated successfully." });
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

        if (dto.PositionId.HasValue &&
            !await _db.Positions.AnyAsync(p => p.Id == dto.PositionId && p.ProfessionId == dto.ProfessionId && p.IsActive))
            return BadRequest(new { message = "Invalid position for the selected profession" });

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
            PositionId = dto.PositionId,
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
