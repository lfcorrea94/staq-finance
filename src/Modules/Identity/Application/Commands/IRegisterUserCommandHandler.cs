using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Identity.Application.DTOs;

namespace StaqFinance.Modules.Identity.Application.Commands;

public interface IRegisterUserCommandHandler
{
    Task<Result<RegisterResponse>> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default);
}
