namespace MedSafe.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    // Links to the new granular Roles table (see Role/Permission/RolePermission).
    // Backfilled from Role above but not yet used for authorization — Role string
    // still drives [Authorize(Roles=...)] and JWT claims until enforcement is wired up.
    public int? RoleId { get; set; }
    public string? Unit { get; set; }
    public string? Title { get; set; }
    public int? ProfessionId { get; set; }
    public string Status { get; set; } = "active";
    public int FailedAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ProfileImage { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
