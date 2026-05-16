using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Accounts.Domain.Entities;
using StaqFinance.Modules.Categories.Domain.Entities;
using StaqFinance.Modules.RecurringTransactions.Application.Commands;
using StaqFinance.Modules.RecurringTransactions.Application.DTOs;
using StaqFinance.Modules.RecurringTransactions.Domain.Entities;

namespace StaqFinance.Modules.RecurringTransactions.Infrastructure.Handlers;

internal sealed class CreateRecurringTransactionCommandHandler : ICreateRecurringTransactionCommandHandler
{
    private readonly DbContext _context;

    public CreateRecurringTransactionCommandHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<RecurringTransactionResponse>> HandleAsync(
        CreateRecurringTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountExists = await _context.Set<Account>()
            .AnyAsync(a => a.Id == command.AccountId, cancellationToken);

        if (!accountExists)
            return Result.Failure<RecurringTransactionResponse>(
                Error.NotFound("Account.NotFound", "Conta não encontrada."));

        if (command.CategoryId.HasValue)
        {
            var categoryExists = await _context.Set<Category>()
                .AnyAsync(c => c.Id == command.CategoryId.Value, cancellationToken);

            if (!categoryExists)
                return Result.Failure<RecurringTransactionResponse>(
                    Error.NotFound("Category.NotFound", "Categoria não encontrada."));
        }

        var recurring = RecurringTransaction.Create(
            command.TenantId,
            command.AccountId,
            command.CategoryId,
            command.Description,
            command.Type,
            command.AmountCents,
            command.StartDate,
            command.EndDate,
            command.Frequency,
            command.Interval);

        _context.Set<RecurringTransaction>().Add(recurring);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToResponse(recurring));
    }

    private static RecurringTransactionResponse MapToResponse(RecurringTransaction r) =>
        new(r.Id, r.AccountId, r.CategoryId, r.Description, r.Type, r.AmountCents,
            r.StartDate, r.EndDate, r.Frequency, r.Interval, r.NextRunOn, r.IsActive, r.CreatedAt);
}
