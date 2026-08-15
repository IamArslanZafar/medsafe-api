using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public string? Unit { get; set; }
    public string? Title { get; set; }
    public int? ProfessionId { get; set; }
    public string? ProfessionName { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ProfileImage { get; set; }
}

public class UpdateUserStatusDto
{
    [Required] public string Status { get; set; } = string.Empty; // active | inactive
}

public class UpdateUserRoleDto
{
    [Required] public int RoleId { get; set; }
}

public class UpdateUserProfessionDto
{
    public int? ProfessionId { get; set; }
    public int? PositionId { get; set; }
}
