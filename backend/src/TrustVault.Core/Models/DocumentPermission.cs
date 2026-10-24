using TrustVault.Core.Enums;

namespace TrustVault.Core.Models;

public class DocumentPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public PermissionType Permission { get; set; }
    public Guid GrantedById { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Document Document { get; set; } = null!;
    public User User { get; set; } = null!;
    public User GrantedBy { get; set; } = null!;
}
