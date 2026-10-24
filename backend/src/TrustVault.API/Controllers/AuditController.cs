using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustVault.API.DTOs.Admin;
using TrustVault.API.Extensions;
using TrustVault.Core.Enums;
using TrustVault.Core.Interfaces;

namespace TrustVault.API.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogListDto>> GetLogs(
        [FromQuery] Guid? documentId,
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        AuditAction? parsedAction = null;
        if (!string.IsNullOrWhiteSpace(action) && Enum.TryParse<AuditAction>(action, true, out var a))
            parsedAction = a;

        var requestingUserId = User.GetUserId();
        var result = await _audit.GetLogsAsync(
            new AuditLogQuery(documentId, userId, parsedAction, from, to, page, pageSize),
            requestingUserId);

        return Ok(new AuditLogListDto
        {
            Logs = result.Logs.Select(AuditLogDto.From),
            Total = result.Total,
            Page = page,
            PageSize = pageSize
        });
    }
}
