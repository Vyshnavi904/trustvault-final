using Microsoft.EntityFrameworkCore;
using TrustVault.Core.Enums;
using TrustVault.Core.Interfaces;
using TrustVault.Core.Models;
using TrustVault.Infrastructure.Data;

namespace TrustVault.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(AuditAction action, bool isSuccess, Guid? userId = null,
        Guid? documentId = null, string ipAddress = "", string userAgent = "", string details = "")
    {
        var log = new AuditLog
        {
            Action = action,
            IsSuccess = isSuccess,
            UserId = userId,
            DocumentId = documentId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<AuditLogResult> GetLogsAsync(AuditLogQuery query, Guid requestingUserId)
    {
        var requestingUser = await _db.Users.FindAsync(requestingUserId)
            ?? throw new UnauthorizedAccessException();

        var q = _db.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Document)
            .AsQueryable();

        if (requestingUser.Role != UserRole.Administrator)
        {
            var ownedDocIds = _db.Documents
                .Where(d => d.OwnerId == requestingUserId)
                .Select(d => d.Id);
            q = q.Where(a => a.DocumentId != null && ownedDocIds.Contains(a.DocumentId!.Value));
        }

        if (query.DocumentId.HasValue)
            q = q.Where(a => a.DocumentId == query.DocumentId);

        if (query.UserId.HasValue)
            q = q.Where(a => a.UserId == query.UserId);

        if (query.Action.HasValue)
            q = q.Where(a => a.Action == query.Action);

        if (query.From.HasValue)
            q = q.Where(a => a.Timestamp >= query.From);

        if (query.To.HasValue)
            q = q.Where(a => a.Timestamp <= query.To);

        var total = await q.CountAsync();
        var logs = await q
            .OrderByDescending(a => a.Timestamp)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new AuditLogResult(logs, total);
    }
}
