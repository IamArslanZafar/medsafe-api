using MedSafe.Models;

namespace MedSafe.Infrastructure.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task AddAsync(User user);
    Task SaveAsync();
    Task AddRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
    Task AddAuditLogAsync(AuditLog log);
    Task<bool> EmailExistsAsync(string email);
}
