using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Accounts.Application.Commands;
using StaqFinance.Modules.Accounts.Application.DTOs;
using StaqFinance.Modules.Accounts.Domain.Entities;

namespace StaqFinance.Modules.Accounts.Infrastructure.Handlers;

internal sealed class CreateAccountCommandHandler : ICreateAccountCommandHandler
{
    private readonly DbContext _context;

    public CreateAccountCommandHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<AccountResponse>> HandleAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.Set<Account>()
            .AnyAsync(a => a.TenantId == command.TenantId && a.Name == command.Name, cancellationToken);

        if (exists)
            return Result.Failure<AccountResponse>(
                Error.Conflict("Account.DuplicateName", $"An account named '{command.Name}' already exists in this workspace."));

        var account = Account.Create(command.TenantId, command.Name);

        _context.Set<Account>().Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new AccountResponse(account.Id, account.Name, account.CreatedAt));
    }
}
