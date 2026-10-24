using TrustVault.Core.Enums;
using TrustVault.Core.Models;

namespace TrustVault.Core.Interfaces;

public record GrantPermissionRequest(Guid DocumentId, Guid UserId, PermissionType Permission, Guid GrantedById);
public record RevokePermissionRequest(Guid PermissionId, Guid RequestingUserId);

public interface IPermissionService
{
    Task<DocumentPermission> GrantAsync(GrantPermissionRequest request);
    Task RevokeAsync(RevokePermissionRequest request);
    Task<IEnumerable<DocumentPermission>> GetByDocumentAsync(Guid documentId, Guid requestingUserId);
    Task<bool> HasPermissionAsync(Guid documentId, Guid userId, PermissionType requiredPermission);
}
