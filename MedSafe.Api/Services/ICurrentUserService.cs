namespace MedSafeAPI.Services;

public interface ICurrentUserService
{
    int UserId { get; }
    string Role { get; }
    string Name { get; }
}
