using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = string.Empty; // Nurse | Physician | Admin
    public string? Unit { get; set; }
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Shift { get; set; }
    public int? ProfessionId { get; set; }
    public int? PositionId { get; set; }
    public IFormFile? ProfileImage { get; set; }
    // Bound from indexed form fields: Availability[0].DayOfWeek, Availability[0].StartTime, ...
    public List<AvailabilityRequestDto> Availability { get; set; } = [];
}

public class RefreshTokenDto
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class ChangeNameDto
{
    [Required] public string NewName { get; set; } = string.Empty;
}

public class ChangePhoneDto
{
    public string? PhoneNumber { get; set; }
}

public class ChangeEmailDto
{
    [Required, EmailAddress] public string NewEmail { get; set; } = string.Empty;
    [Required] public string CurrentPassword { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Shift { get; set; }
    public int? ProfessionId { get; set; }
    public string? ProfessionName { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public string? ProfileImage { get; set; }
    // Permission tags granted to the user's Role (e.g. "alert_rules.manage") — drives
    // frontend button/section gating so it stays in sync with whatever an Admin configures
    // in Roles & Permissions, instead of a hardcoded per-role map baked into the bundle.
    public List<string> Permissions { get; set; } = new();
}

public class RefreshResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
