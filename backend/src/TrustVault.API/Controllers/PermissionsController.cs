using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustVault.API.DTOs.Permissions;
using TrustVault.API.Extensions;
using TrustVault.Core.Enums;
using TrustVault.Core.Interfaces;

namespace TrustVault.API.Controllers;

[ApiController]
[Route("api/documents/{documentId:guid}/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissions;

    public PermissionsController(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions(Guid documentId)
    {
        var userId = User.GetUserId();
        var perms = await _permissions.GetByDocumentAsync(documentId, userId);
        return Ok(perms.Select(PermissionDto.From));
    }

    [HttpPost]
    public async Task<ActionResult<PermissionDto>> Grant(Guid documentId, [FromBody] GrantPermissionDto dto)
    {
        var userId = User.GetUserId();

        if (!Enum.TryParse<PermissionType>(dto.Permission, true, out var permType))
            return BadRequest(new { error = "Permission must be 'Read' or 'Edit'." });

        var perm = await _permissions.GrantAsync(new GrantPermissionRequest(
            documentId, dto.UserId, permType, userId));

        return Ok(PermissionDto.From(perm));
    }

    [HttpDelete("{permissionId:guid}")]
    public async Task<IActionResult> Revoke(Guid documentId, Guid permissionId)
    {
        var userId = User.GetUserId();
        await _permissions.RevokeAsync(new RevokePermissionRequest(permissionId, userId));
        return NoContent();
    }
}
