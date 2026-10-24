using System.ComponentModel.DataAnnotations;
using TrustVault.Core.Models;

namespace TrustVault.API.DTOs.Permissions;

public class GrantPermissionDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Permission { get; set; } = "Read";
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public Guid GrantedById { get; set; }
    public string GrantedByEmail { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }

    public static PermissionDto From(DocumentPermission p) => new()
    {
        Id = p.Id, DocumentId = p.DocumentId, UserId = p.UserId,
        UserEmail = p.User?.Email ?? "", UserFullName = p.User?.FullName ?? "",
        Permission = p.Permission.ToString(),
        GrantedById = p.GrantedById, GrantedByEmail = p.GrantedBy?.Email ?? "",
        GrantedAt = p.GrantedAt
    };
}
