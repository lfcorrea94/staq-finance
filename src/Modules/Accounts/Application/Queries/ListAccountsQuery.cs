using StaqFinance.Modules.Accounts.Application.DTOs;
using StaqFinance.BuildingBlocks;

namespace StaqFinance.Modules.Accounts.Application.Queries;

public sealed record ListAccountsQuery(Guid TenantId);

public interface IListAccountsQueryHandler
{
    Task<Result<IReadOnlyList<AccountResponse>>> HandleAsync(ListAccountsQuery query, CancellationToken cancellationToken = default);
}
