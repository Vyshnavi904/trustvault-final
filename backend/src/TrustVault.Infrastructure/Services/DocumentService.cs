using Microsoft.EntityFrameworkCore;
using TrustVault.Core.Enums;
using TrustVault.Core.Interfaces;
using TrustVault.Core.Models;
using TrustVault.Infrastructure.Data;

namespace TrustVault.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IPermissionService _permissions;
    private readonly IAuditService _audit;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "text/csv",
        "image/png",
        "image/jpeg",
        "image/gif",
        "application/zip"
    };

    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    public DocumentService(AppDbContext db, IFileStorageService storage,
        IPermissionService permissions, IAuditService audit)
    {
        _db = db;
        _storage = storage;
        _permissions = permissions;
        _audit = audit;
    }

    public async Task<Document> UploadAsync(DocumentUploadRequest request)
    {
        var owner = await _db.Users.FindAsync(request.OwnerId)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (owner.Role != UserRole.Owner && owner.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException("Only Owners and Administrators can upload documents.");

        if (request.FileSize == 0)
            throw new ArgumentException("File cannot be empty.");

        if (request.FileSize > MaxFileSizeBytes)
            throw new ArgumentException("File exceeds the 50 MB size limit.");

        if (!AllowedMimeTypes.Contains(request.ContentType))
            throw new ArgumentException($"File type '{request.ContentType}' is not allowed.");

        var storedPath = $"{request.OwnerId}/{Guid.NewGuid()}/{request.OriginalFileName}";
        await _storage.UploadAsync(request.FileStream, storedPath, request.ContentType);

        var document = new Document
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            OriginalFileName = request.OriginalFileName,
            StoredPath = storedPath,
            MimeType = request.ContentType,
            FileSizeBytes = request.FileSize,
            OwnerId = request.OwnerId
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.DocumentUploaded, true, request.OwnerId, document.Id,
            details: $"Uploaded '{document.Title}' ({document.OriginalFileName}, {document.FileSizeBytes} bytes)");

        return document;
    }

    public async Task<Document> GetByIdAsync(Guid documentId, Guid requestingUserId)
    {
        var hasAccess = await _permissions.HasPermissionAsync(documentId, requestingUserId, PermissionType.Read);
        if (!hasAccess)
        {
            await _audit.LogAsync(AuditAction.AccessDenied, false, requestingUserId, documentId,
                details: "Attempted to view document metadata without permission.");
            throw new UnauthorizedAccessException("Access denied.");
        }

        var document = await _db.Documents
            .Include(d => d.Owner)
            .FirstOrDefaultAsync(d => d.Id == documentId)
            ?? throw new KeyNotFoundException("Document not found.");

        await _audit.LogAsync(AuditAction.DocumentViewed, true, requestingUserId, documentId);
        return document;
    }

    public async Task<DocumentSearchResult> SearchAsync(DocumentSearchQuery query, Guid requestingUserId)
    {
        var user = await _db.Users.FindAsync(requestingUserId)
            ?? throw new UnauthorizedAccessException();

        IQueryable<Document> q;

        if (user.Role == UserRole.Administrator)
        {
            q = _db.Documents.Include(d => d.Owner);
        }
        else if (user.Role == UserRole.Owner)
        {
            var accessibleIds = _db.DocumentPermissions
                .Where(p => p.UserId == requestingUserId && p.IsActive)
                .Select(p => p.DocumentId);

            q = _db.Documents
                .Include(d => d.Owner)
                .Where(d => d.OwnerId == requestingUserId || accessibleIds.Contains(d.Id));
        }
        else
        {
            var accessibleIds = _db.DocumentPermissions
                .Where(p => p.UserId == requestingUserId && p.IsActive)
                .Select(p => p.DocumentId);

            q = _db.Documents
                .Include(d => d.Owner)
                .Where(d => accessibleIds.Contains(d.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Query))
            q = q.Where(d => d.Title.Contains(query.Query) || d.Description.Contains(query.Query)
                || d.OriginalFileName.Contains(query.Query));

        if (!string.IsNullOrWhiteSpace(query.Category))
            q = q.Where(d => d.Category == query.Category);

        if (query.OwnerId.HasValue)
            q = q.Where(d => d.OwnerId == query.OwnerId);

        if (query.From.HasValue)
            q = q.Where(d => d.CreatedAt >= query.From);

        if (query.To.HasValue)
            q = q.Where(d => d.CreatedAt <= query.To);

        var total = await q.CountAsync();
        var docs = await q
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new DocumentSearchResult(docs, total);
    }

    public async Task<Document> UpdateAsync(DocumentUpdateRequest request)
    {
        var hasEdit = await _permissions.HasPermissionAsync(request.DocumentId, request.RequestingUserId, PermissionType.Edit);
        if (!hasEdit)
        {
            await _audit.LogAsync(AuditAction.AccessDenied, false, request.RequestingUserId, request.DocumentId,
                details: "Attempted to edit document without permission.");
            throw new UnauthorizedAccessException("Access denied.");
        }

        var document = await _db.Documents.FindAsync(request.DocumentId)
            ?? throw new KeyNotFoundException("Document not found.");

        document.Title = request.Title.Trim();
        document.Description = request.Description.Trim();
        document.Category = request.Category.Trim();
        document.UpdatedAt = DateTime.UtcNow;

        if (request.FileStream != null && request.OriginalFileName != null && request.ContentType != null)
        {
            if (request.FileSize > MaxFileSizeBytes)
                throw new ArgumentException("File exceeds the 50 MB size limit.");

            if (!AllowedMimeTypes.Contains(request.ContentType))
                throw new ArgumentException($"File type '{request.ContentType}' is not allowed.");

            await _storage.DeleteAsync(document.StoredPath);
            var newPath = $"{document.OwnerId}/{Guid.NewGuid()}/{request.OriginalFileName}";
            await _storage.UploadAsync(request.FileStream, newPath, request.ContentType);

            document.StoredPath = newPath;
            document.OriginalFileName = request.OriginalFileName;
            document.MimeType = request.ContentType;
            document.FileSizeBytes = request.FileSize;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditAction.DocumentEdited, true, request.RequestingUserId, document.Id,
            details: $"Updated document '{document.Title}'");

        return document;
    }

    public async Task DeleteAsync(Guid documentId, Guid requestingUserId)
    {
        var document = await _db.Documents.FindAsync(documentId)
            ?? throw new KeyNotFoundException("Document not found.");

        var user = await _db.Users.FindAsync(requestingUserId)
            ?? throw new UnauthorizedAccessException();

        if (document.OwnerId != requestingUserId && user.Role != UserRole.Administrator)
        {
            await _audit.LogAsync(AuditAction.AccessDenied, false, requestingUserId, documentId,
                details: "Attempted to delete document without ownership.");
            throw new UnauthorizedAccessException("Only the document owner can delete it.");
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.DocumentDeleted, true, requestingUserId, documentId,
            details: $"Deleted document '{document.Title}'");
    }

    public async Task<DocumentDownloadResult> DownloadAsync(Guid documentId, Guid requestingUserId,
        string ipAddress, string userAgent)
    {
        var hasAccess = await _permissions.HasPermissionAsync(documentId, requestingUserId, PermissionType.Read);
        if (!hasAccess)
        {
            await _audit.LogAsync(AuditAction.AccessDenied, false, requestingUserId, documentId,
                ipAddress: ipAddress, userAgent: userAgent,
                details: "Attempted to download document without permission.");
            throw new UnauthorizedAccessException("Access denied.");
        }

        var document = await _db.Documents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == documentId)
            ?? throw new KeyNotFoundException("Document not found.");

        await _audit.LogAsync(AuditAction.DocumentDownloaded, true, requestingUserId, documentId,
            ipAddress: ipAddress, userAgent: userAgent,
            details: $"Downloaded '{document.OriginalFileName}'");

        var stream = await _storage.DownloadAsync(document.StoredPath);
        return new DocumentDownloadResult(stream, document.OriginalFileName, document.MimeType);
    }
}
