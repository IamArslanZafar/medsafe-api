namespace MedSafeAPI.Services;

public interface IFileService
{
    Task<string?> SaveProfileImageAsync(IFormFile image);
}
