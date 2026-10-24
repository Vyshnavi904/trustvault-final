using TrustVault.Core.Models;

namespace TrustVault.Core.Interfaces;

public record DocumentUploadRequest(
    string Title, string Description, string Category,
    Stream FileStream, string OriginalFileName, string ContentType,
    long FileSize, Guid OwnerId);

public record DocumentUpdateRequest(
    Guid DocumentId, string Title, string Description,
    string Category, Guid RequestingUserId,
    Stream? FileStream = null, string? OriginalFileName = null,
    string? ContentType = null, long FileSize = 0);

public record DocumentSearchQuery(
    string? Query, string? Category, Guid? OwnerId,
    DateTime? From, DateTime? To, int Page = 1, int PageSize = 20);

public record DocumentSearchResult(IEnumerable<Document> Documents, int Total);

public record DocumentDownloadResult(Stream FileStream, string FileName, string MimeType);

public interface IDocumentService
{
    Task<Document> UploadAsync(DocumentUploadRequest request);
    Task<Document> GetByIdAsync(Guid documentId, Guid requestingUserId);
    Task<DocumentSearchResult> SearchAsync(DocumentSearchQuery query, Guid requestingUserId);
    Task<Document> UpdateAsync(DocumentUpdateRequest request);
    Task DeleteAsync(Guid documentId, Guid requestingUserId);
    Task<DocumentDownloadResult> DownloadAsync(Guid documentId, Guid requestingUserId, string ipAddress, string userAgent);
}
