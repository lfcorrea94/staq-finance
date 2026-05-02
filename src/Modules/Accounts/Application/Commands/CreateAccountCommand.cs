using StaqFinance.Modules.Accounts.Application.DTOs;
using StaqFinance.BuildingBlocks;

namespace StaqFinance.Modules.Accounts.Application.Commands;

public sealed record CreateAccountCommand(Guid TenantId, string Name);

public interface ICreateAccountCommandHandler
{
    Task<Result<AccountResponse>> HandleAsync(CreateAccountCommand command, CancellationToken cancellationToken = default);
}
