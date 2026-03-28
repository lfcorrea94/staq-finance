using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StaqFinance.Modules.Identity.Application.Services;
using StaqFinance.Modules.Identity.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StaqFinance.Modules.Identity.Infrastructure.Services;

internal sealed class TokenService : ITokenService
{
    private readonly DbContext _context;
    private readonly IConfiguration _configuration;

    public TokenService(DbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public string GenerateAccessToken(ApplicationUser user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var expiresInMinutes = int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var min) ? min : 60;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var expiresInDays = int.TryParse(_configuration["Jwt:RefreshExpiresInDays"], out var days) ? days : 7;
        var tokenValue = GenerateSecureToken();
        var refreshToken = RefreshToken.Create(userId, tokenValue, expiresInDays);

        _context.Set<RefreshToken>().Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return tokenValue;
    }

    public async Task<(bool IsValid, Guid UserId, string NewToken)> ValidateAndRotateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (existing is null || !existing.IsValid)
            return (false, Guid.Empty, string.Empty);

        existing.Revoke();

        var expiresInDays = int.TryParse(_configuration["Jwt:RefreshExpiresInDays"], out var days) ? days : 7;
        var newTokenValue = GenerateSecureToken();
        var newToken = RefreshToken.Create(existing.UserId, newTokenValue, expiresInDays);

        _context.Set<RefreshToken>().Add(newToken);
        await _context.SaveChangesAsync(cancellationToken);

        return (true, existing.UserId, newTokenValue);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
