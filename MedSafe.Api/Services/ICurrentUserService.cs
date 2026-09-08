namespace MedSafeAPI.Services;

public interface ICurrentUserService
{
    int UserId { get; }
    string Role { get; }
    string Name { get; }
    // True when this user was granted the "Admin Data Access" toggle (User.HasFullDataAccess),
    // separate from Role=="Admin" — checks that scope non-Admin roles to their own records
    // (Reports Hub, dashboard, etc.) should also treat this as "sees everything".
    bool HasFullDataAccess { get; }
}
