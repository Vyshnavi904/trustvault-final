using System.Security.Claims;

namespace TrustVault.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(id);
    }

    public static string GetIpAddress(this HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static string GetUserAgent(this HttpContext context)
        => context.Request.Headers.UserAgent.ToString();
}
