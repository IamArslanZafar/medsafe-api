using System.Security.Claims;

namespace MedSafeAPI.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public int UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : throw new InvalidOperationException("No authenticated user.");
        }
    }

    public string Role => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    public string Name => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
}
