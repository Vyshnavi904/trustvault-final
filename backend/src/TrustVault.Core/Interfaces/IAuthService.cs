using TrustVault.Core.Models;

namespace TrustVault.Core.Interfaces;

public record RegisterRequest(string Email, string Password, string FullName, string Role);
public record LoginRequest(string Email, string Password);
public record AuthTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt, User User);
public record RefreshRequest(string RefreshToken);

public interface IAuthService
{
    Task<AuthTokens> RegisterAsync(RegisterRequest request);
    Task<AuthTokens> LoginAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<AuthTokens> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
