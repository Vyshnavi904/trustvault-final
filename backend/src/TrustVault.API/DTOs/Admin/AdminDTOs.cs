using System.ComponentModel.DataAnnotations;
using TrustVault.Core.Models;

namespace TrustVault.API.DTOs.Admin;

public class SetUserActiveDto
{
    public bool IsActive { get; set; }
}

public class ChangeRoleDto
{
    [Required]
    public string Role { get; set; } = string.Empty;
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public static AuditLogDto From(AuditLog a) => new()
    {
        Id = a.Id, DocumentId = a.DocumentId,
        DocumentTitle = a.Document?.Title ?? "",
        UserId = a.UserId, UserEmail = a.User?.Email ?? "",
        Action = a.Action.ToString(), IsSuccess = a.IsSuccess,
        IpAddress = a.IpAddress, Details = a.Details, Timestamp = a.Timestamp
    };
}

public class AuditLogListDto
{
    public IEnumerable<AuditLogDto> Logs { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
