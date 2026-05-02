using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Accounts.Application.DTOs;
using StaqFinance.Modules.Accounts.Application.Queries;
using StaqFinance.Modules.Accounts.Domain.Entities;

namespace StaqFinance.Modules.Accounts.Infrastructure.Handlers;

internal sealed class ListAccountsQueryHandler : IListAccountsQueryHandler
{
    private readonly DbContext _context;

    public ListAccountsQueryHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<AccountResponse>>> HandleAsync(
        ListAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _context.Set<Account>()
            .OrderBy(a => a.Name)
            .Select(a => new AccountResponse(a.Id, a.Name, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AccountResponse>>(accounts);
    }
}
