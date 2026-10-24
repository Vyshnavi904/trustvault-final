using Microsoft.EntityFrameworkCore;
using TrustVault.Core.Enums;
using TrustVault.Core.Interfaces;
using TrustVault.Core.Models;
using TrustVault.Infrastructure.Data;

namespace TrustVault.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public PermissionService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<DocumentPermission> GrantAsync(GrantPermissionRequest request)
    {
        var document = await _db.Documents.FindAsync(request.DocumentId)
            ?? throw new KeyNotFoundException("Document not found.");

        var requestingUser = await _db.Users.FindAsync(request.GrantedById)
            ?? throw new UnauthorizedAccessException();

        if (document.OwnerId != request.GrantedById && requestingUser.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException("Only the document owner can grant permissions.");

        var targetUser = await _db.Users.FindAsync(request.UserId)
            ?? throw new KeyNotFoundException("Target user not found.");

        var existing = await _db.DocumentPermissions
            .FirstOrDefaultAsync(p => p.DocumentId == request.DocumentId
                && p.UserId == request.UserId && p.IsActive);

        if (existing != null)
        {
            existing.Permission = request.Permission;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(AuditAction.PermissionGranted, true, request.GrantedById,
                request.DocumentId,
                details: $"Updated permission to {request.Permission} for user {targetUser.Email}");

            return existing;
        }

        var permission = new DocumentPermission
        {
            DocumentId = request.DocumentId,
            UserId = request.UserId,
            Permission = request.Permission,
            GrantedById = request.GrantedById
        };

        _db.DocumentPermissions.Add(permission);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.PermissionGranted, true, request.GrantedById,
            request.DocumentId,
            details: $"Granted {request.Permission} to user {targetUser.Email}");

        return permission;
    }

    public async Task RevokeAsync(RevokePermissionRequest request)
    {
        var permission = await _db.DocumentPermissions
            .Include(p => p.Document)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId)
            ?? throw new KeyNotFoundException("Permission not found.");

        var requestingUser = await _db.Users.FindAsync(request.RequestingUserId)
            ?? throw new UnauthorizedAccessException();

        if (permission.Document.OwnerId != request.RequestingUserId
            && requestingUser.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException("Only the document owner can revoke permissions.");

        permission.IsActive = false;
        permission.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.PermissionRevoked, true, request.RequestingUserId,
            permission.DocumentId,
            details: $"Revoked permission from user {permission.User.Email}");
    }

    public async Task<IEnumerable<DocumentPermission>> GetByDocumentAsync(Guid documentId, Guid requestingUserId)
    {
        var document = await _db.Documents.FindAsync(documentId)
            ?? throw new KeyNotFoundException("Document not found.");

        var requestingUser = await _db.Users.FindAsync(requestingUserId)
            ?? throw new UnauthorizedAccessException();

        if (document.OwnerId != requestingUserId && requestingUser.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException("Access denied.");

        return await _db.DocumentPermissions
            .Include(p => p.User)
            .Include(p => p.GrantedBy)
            .Where(p => p.DocumentId == documentId && p.IsActive)
            .ToListAsync();
    }

    public async Task<bool> HasPermissionAsync(Guid documentId, Guid userId, PermissionType requiredPermission)
    {
        var document = await _db.Documents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.IsDeleted) return false;
        if (document.OwnerId == userId) return true;

        var user = await _db.Users.FindAsync(userId);
        if (user == null || !user.IsActive) return false;
        if (user.Role == UserRole.Administrator) return true;

        var permission = await _db.DocumentPermissions
            .FirstOrDefaultAsync(p => p.DocumentId == documentId
                && p.UserId == userId && p.IsActive);

        if (permission == null) return false;

        return requiredPermission == PermissionType.Read
            || permission.Permission == PermissionType.Edit;
    }
}
