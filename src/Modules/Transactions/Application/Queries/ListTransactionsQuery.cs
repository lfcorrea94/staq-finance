using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Transactions.Application.DTOs;

namespace StaqFinance.Modules.Transactions.Application.Queries;

public sealed record ListTransactionsQuery(Guid TenantId, ListTransactionsFilter Filter);

public interface IListTransactionsQueryHandler
{
    Task<Result<IReadOnlyList<TransactionResponse>>> HandleAsync(ListTransactionsQuery query, CancellationToken cancellationToken = default);
}
