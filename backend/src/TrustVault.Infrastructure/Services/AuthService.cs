using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TrustVault.Core.Enums;
using TrustVault.Core.Interfaces;
using TrustVault.Core.Models;
using TrustVault.Infrastructure.Data;

namespace TrustVault.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditService _audit;

    public AuthService(AppDbContext db, IConfiguration config, IAuditService audit)
    {
        _db = db;
        _config = config;
        _audit = audit;
    }

    public async Task<AuthTokens> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            throw new InvalidOperationException("Email already registered.");

        var role = request.Role?.ToLower() == "owner" ? UserRole.Owner : UserRole.Viewer;

        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Role = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.Register, true, user.Id, details: $"User registered as {role}");

        return await IssueTokensAsync(user);
    }

    public async Task<AuthTokens> LoginAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await _audit.LogAsync(AuditAction.Login, false, ipAddress: ipAddress, userAgent: userAgent,
                details: $"Failed login attempt for email: {request.Email}");
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        await _audit.LogAsync(AuditAction.Login, true, user.Id, ipAddress: ipAddress, userAgent: userAgent);
        return await IssueTokensAsync(user);
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

        if (stored == null || !stored.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await IssueTokensAsync(stored.User);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
        if (stored != null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(AuditAction.Logout, true, stored.UserId);
        }
    }

    private async Task<AuthTokens> IssueTokensAsync(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var (rawRefreshToken, tokenHash) = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        return new AuthTokens(accessToken, rawRefreshToken, expiresAt, user);
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("fullName", user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static (string raw, string hash) GenerateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (raw, HashToken(raw));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
