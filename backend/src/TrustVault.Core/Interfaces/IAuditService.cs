using TrustVault.Core.Enums;
using TrustVault.Core.Models;

namespace TrustVault.Core.Interfaces;

public record AuditLogQuery(
    Guid? DocumentId, Guid? UserId, AuditAction? Action,
    DateTime? From, DateTime? To, int Page = 1, int PageSize = 50);

public record AuditLogResult(IEnumerable<AuditLog> Logs, int Total);

public interface IAuditService
{
    Task LogAsync(AuditAction action, bool isSuccess, Guid? userId = null, Guid? documentId = null,
        string ipAddress = "", string userAgent = "", string details = "");
    Task<AuditLogResult> GetLogsAsync(AuditLogQuery query, Guid requestingUserId);
}
