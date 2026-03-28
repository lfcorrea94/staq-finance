using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Identity.Application.DTOs;

namespace StaqFinance.Modules.Identity.Application.Commands;

public interface IRefreshTokenCommandHandler
{
    Task<Result<AuthResponse>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default);
}
