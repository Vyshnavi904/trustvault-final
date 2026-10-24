using Microsoft.EntityFrameworkCore;
using TrustVault.Core.Enums;
using TrustVault.Core.Interfaces;
using TrustVault.Core.Models;
using TrustVault.Infrastructure.Data;

namespace TrustVault.Infrastructure.Services;

public class AdminService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public AdminService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private async Task EnsureAdminAsync(Guid requestingUserId)
    {
        var user = await _db.Users.FindAsync(requestingUserId)
            ?? throw new UnauthorizedAccessException();
        if (user.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException("Administrator access required.");
    }

    public async Task<IEnumerable<User>> GetUsersAsync(Guid requestingUserId)
    {
        await EnsureAdminAsync(requestingUserId);
        return await _db.Users.OrderBy(u => u.FullName).ToListAsync();
    }

    public async Task<User> SetUserActiveAsync(Guid targetUserId, bool isActive, Guid requestingUserId)
    {
        await EnsureAdminAsync(requestingUserId);

        var user = await _db.Users.FindAsync(targetUserId)
            ?? throw new KeyNotFoundException("User not found.");

        user.IsActive = isActive;
        await _db.SaveChangesAsync();

        var action = isActive ? AuditAction.UserActivated : AuditAction.UserDeactivated;
        await _audit.LogAsync(action, true, requestingUserId,
            details: $"Set user {user.Email} active={isActive}");

        return user;
    }

    public async Task<User> ChangeUserRoleAsync(Guid targetUserId, UserRole newRole, Guid requestingUserId)
    {
        await EnsureAdminAsync(requestingUserId);

        var user = await _db.Users.FindAsync(targetUserId)
            ?? throw new KeyNotFoundException("User not found.");

        var oldRole = user.Role;
        user.Role = newRole;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.UserRoleChanged, true, requestingUserId,
            details: $"Changed {user.Email} role from {oldRole} to {newRole}");

        return user;
    }

    public async Task<IEnumerable<Document>> GetAllDocumentsAsync(Guid requestingUserId)
    {
        await EnsureAdminAsync(requestingUserId);
        return await _db.Documents
            .Include(d => d.Owner)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
