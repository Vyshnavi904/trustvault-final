using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustVault.API.DTOs.Admin;
using TrustVault.API.DTOs.Auth;
using TrustVault.API.DTOs.Documents;
using TrustVault.API.Extensions;
using TrustVault.Core.Enums;
using TrustVault.Infrastructure.Services;

namespace TrustVault.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrator")]
public class AdminController : ControllerBase
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin)
    {
        _admin = admin;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var requestingUserId = User.GetUserId();
        var users = await _admin.GetUsersAsync(requestingUserId);
        return Ok(users.Select(UserDto.From));
    }

    [HttpPatch("users/{userId:guid}/active")]
    public async Task<ActionResult<UserDto>> SetActive(Guid userId, [FromBody] SetUserActiveDto dto)
    {
        var requestingUserId = User.GetUserId();
        var user = await _admin.SetUserActiveAsync(userId, dto.IsActive, requestingUserId);
        return Ok(UserDto.From(user));
    }

    [HttpPatch("users/{userId:guid}/role")]
    public async Task<ActionResult<UserDto>> ChangeRole(Guid userId, [FromBody] ChangeRoleDto dto)
    {
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
            return BadRequest(new { error = "Role must be 'Viewer', 'Owner', or 'Administrator'." });

        var requestingUserId = User.GetUserId();
        var user = await _admin.ChangeUserRoleAsync(userId, role, requestingUserId);
        return Ok(UserDto.From(user));
    }

    [HttpGet("documents")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAllDocuments()
    {
        var requestingUserId = User.GetUserId();
        var docs = await _admin.GetAllDocumentsAsync(requestingUserId);
        return Ok(docs.Select(DocumentDto.From));
    }
}
