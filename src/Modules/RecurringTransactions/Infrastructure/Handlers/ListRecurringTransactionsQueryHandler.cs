using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.RecurringTransactions.Application.DTOs;
using StaqFinance.Modules.RecurringTransactions.Application.Queries;
using StaqFinance.Modules.RecurringTransactions.Domain.Entities;

namespace StaqFinance.Modules.RecurringTransactions.Infrastructure.Handlers;

internal sealed class ListRecurringTransactionsQueryHandler : IListRecurringTransactionsQueryHandler
{
    private readonly DbContext _context;

    public ListRecurringTransactionsQueryHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<RecurringTransactionResponse>>> HandleAsync(
        ListRecurringTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _context.Set<RecurringTransaction>().AsQueryable();

        if (query.Filter.IsActive.HasValue)
            q = q.Where(r => r.IsActive == query.Filter.IsActive.Value);

        var items = await q
            .OrderBy(r => r.NextRunOn)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new RecurringTransactionResponse(
                r.Id, r.AccountId, r.CategoryId, r.Description, r.Type, r.AmountCents,
                r.StartDate, r.EndDate, r.Frequency, r.Interval, r.NextRunOn, r.IsActive, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RecurringTransactionResponse>>(items);
    }
}
