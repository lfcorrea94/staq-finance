using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.RecurringTransactions.Application.DTOs;

namespace StaqFinance.Modules.RecurringTransactions.Application.Queries;

public sealed record ListRecurringTransactionsQuery(Guid TenantId, ListRecurringTransactionsFilter Filter);

public interface IListRecurringTransactionsQueryHandler
{
    Task<Result<IReadOnlyList<RecurringTransactionResponse>>> HandleAsync(ListRecurringTransactionsQuery query, CancellationToken cancellationToken = default);
}
