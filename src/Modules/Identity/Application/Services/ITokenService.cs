using StaqFinance.Modules.Identity.Domain.Entities;

namespace StaqFinance.Modules.Identity.Application.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(bool IsValid, Guid UserId, string NewToken)> ValidateAndRotateAsync(
        string token,
        CancellationToken cancellationToken = default);
}
