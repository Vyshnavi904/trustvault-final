using System.ComponentModel.DataAnnotations;
using TrustVault.Core.Models;

namespace TrustVault.API.DTOs.Documents;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static DocumentDto From(Document d) => new()
    {
        Id = d.Id, Title = d.Title, Description = d.Description,
        Category = d.Category, OriginalFileName = d.OriginalFileName,
        MimeType = d.MimeType, FileSizeBytes = d.FileSizeBytes,
        OwnerId = d.OwnerId,
        OwnerName = d.Owner?.FullName ?? "",
        OwnerEmail = d.Owner?.Email ?? "",
        CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt
    };
}

public class DocumentListDto
{
    public IEnumerable<DocumentDto> Documents { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UpdateDocumentDto
{
    [Required, MinLength(1)]
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
