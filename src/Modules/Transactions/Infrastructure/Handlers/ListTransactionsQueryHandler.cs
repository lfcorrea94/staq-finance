using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Transactions.Application.DTOs;
using StaqFinance.Modules.Transactions.Application.Queries;
using StaqFinance.Modules.Transactions.Domain.Entities;

namespace StaqFinance.Modules.Transactions.Infrastructure.Handlers;

internal sealed class ListTransactionsQueryHandler : IListTransactionsQueryHandler
{
    private readonly DbContext _context;

    public ListTransactionsQueryHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<TransactionResponse>>> HandleAsync(
        ListTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _context.Set<Transaction>().AsQueryable();

        var filter = query.Filter;

        if (filter.From.HasValue)
            q = q.Where(t => t.Date >= filter.From.Value);

        if (filter.To.HasValue)
            q = q.Where(t => t.Date <= filter.To.Value);

        if (filter.AccountId.HasValue)
            q = q.Where(t => t.AccountId == filter.AccountId.Value);

        if (filter.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == filter.CategoryId.Value);

        if (filter.Type.HasValue)
            q = q.Where(t => t.Type == filter.Type.Value);

        var transactions = await q
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => new TransactionResponse(
                t.Id,
                t.AccountId,
                t.CategoryId,
                t.Date,
                t.Description,
                t.Type,
                t.AmountCents,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TransactionResponse>>(transactions);
    }
}
