using TrustVault.Core.Enums;

namespace TrustVault.Core.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? DocumentId { get; set; }
    public Guid? UserId { get; set; }
    public AuditAction Action { get; set; }
    public bool IsSuccess { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Document? Document { get; set; }
    public User? User { get; set; }
}
