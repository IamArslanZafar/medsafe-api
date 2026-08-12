namespace MedSafeAPI.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;

    public FileService(IWebHostEnvironment env) => _env = env;

    public async Task<string?> SaveProfileImageAsync(IFormFile image)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "imageprofile");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var filePath = Path.Combine(folder, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);
        return $"/imageprofile/{fileName}";
    }
}
