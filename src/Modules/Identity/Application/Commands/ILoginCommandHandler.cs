using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Identity.Application.DTOs;

namespace StaqFinance.Modules.Identity.Application.Commands;

public interface ILoginCommandHandler
{
    Task<Result<AuthResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default);
}
