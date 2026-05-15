using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Accounts.Domain.Entities;
using StaqFinance.Modules.Categories.Domain.Entities;
using StaqFinance.Modules.Transactions.Application.Commands;
using StaqFinance.Modules.Transactions.Application.DTOs;
using StaqFinance.Modules.Transactions.Domain.Entities;

namespace StaqFinance.Modules.Transactions.Infrastructure.Handlers;

internal sealed class CreateTransactionCommandHandler : ICreateTransactionCommandHandler
{
    private readonly DbContext _context;

    public CreateTransactionCommandHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<TransactionResponse>> HandleAsync(
        CreateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountExists = await _context.Set<Account>()
            .AnyAsync(a => a.Id == command.AccountId, cancellationToken);

        if (!accountExists)
            return Result.Failure<TransactionResponse>(
                Error.NotFound("Account.NotFound", "Conta não encontrada."));

        if (command.CategoryId.HasValue)
        {
            var categoryExists = await _context.Set<Category>()
                .AnyAsync(c => c.Id == command.CategoryId.Value, cancellationToken);

            if (!categoryExists)
                return Result.Failure<TransactionResponse>(
                    Error.NotFound("Category.NotFound", "Categoria não encontrada."));
        }

        var transaction = Transaction.Create(
            command.TenantId,
            command.AccountId,
            command.CategoryId,
            command.Date,
            command.Description,
            command.Type,
            command.AmountCents);

        _context.Set<Transaction>().Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new TransactionResponse(
            transaction.Id,
            transaction.AccountId,
            transaction.CategoryId,
            transaction.Date,
            transaction.Description,
            transaction.Type,
            transaction.AmountCents,
            transaction.CreatedAt));
    }
}
