using TrustVault.Core.Enums;

namespace TrustVault.Core.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Document> OwnedDocuments { get; set; } = new List<Document>();
    public ICollection<DocumentPermission> GrantedPermissions { get; set; } = new List<DocumentPermission>();
    public ICollection<DocumentPermission> ReceivedPermissions { get; set; } = new List<DocumentPermission>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
