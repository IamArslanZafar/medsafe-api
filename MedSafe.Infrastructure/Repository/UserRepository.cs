using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Infrastructure.Interfaces;
using MedSafe.Models;

namespace MedSafe.Infrastructure.Repository;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(int id) =>
        await _db.Users.FindAsync(id);

    public async Task<List<User>> GetAllAsync() =>
        await _db.Users.OrderBy(u => u.Name).ToListAsync();

    public async Task AddAsync(User user) => await _db.Users.AddAsync(user);

    public async Task SaveAsync() => await _db.SaveChangesAsync();

    public async Task AddRefreshTokenAsync(RefreshToken token) => await _db.RefreshTokens.AddAsync(token);

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token) =>
        await _db.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token && !r.IsRevoked);

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (stored != null) stored.IsRevoked = true;
    }

    public async Task AddAuditLogAsync(AuditLog log) => await _db.AuditLogs.AddAsync(log);

    public async Task<bool> EmailExistsAsync(string email) =>
        await _db.Users.AnyAsync(u => u.Email == email);
}
