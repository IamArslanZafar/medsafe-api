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
    public int? ProfessionId { get; set; }
    public IFormFile? ProfileImage { get; set; }
}

public class RefreshTokenDto
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
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
    public int? ProfessionId { get; set; }
}

public class RefreshResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
