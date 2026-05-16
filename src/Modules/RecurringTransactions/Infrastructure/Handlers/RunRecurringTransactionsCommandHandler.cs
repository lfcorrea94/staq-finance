using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.RecurringTransactions.Application.Commands;
using StaqFinance.Modules.RecurringTransactions.Application.DTOs;
using StaqFinance.Modules.RecurringTransactions.Domain.Entities;
using StaqFinance.Modules.Transactions.Domain.Entities;

namespace StaqFinance.Modules.RecurringTransactions.Infrastructure.Handlers;

internal sealed class RunRecurringTransactionsCommandHandler : IRunRecurringTransactionsCommandHandler
{
    private readonly DbContext _context;

    public RunRecurringTransactionsCommandHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<RunRecurringTransactionsResponse>> HandleAsync(
        RunRecurringTransactionsCommand command,
        CancellationToken cancellationToken = default)
    {
        var actives = await _context.Set<RecurringTransaction>()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        // Load existing runs for these recurring transactions to check idempotency
        var activeIds = actives.Select(r => r.Id).ToList();
        var existingRunSet = (await _context.Set<RecurringTransactionRun>()
            .Where(run => activeIds.Contains(run.RecurringTransactionId))
            .Select(run => new { run.RecurringTransactionId, run.RunDate })
            .ToListAsync(cancellationToken))
            .Select(x => (x.RecurringTransactionId, x.RunDate))
            .ToHashSet();

        int processedRules = 0;
        int generatedTransactions = 0;

        foreach (var recurring in actives)
        {
            processedRules++;

            while (recurring.NextRunOn <= command.Until &&
                   (recurring.EndDate is null || recurring.NextRunOn <= recurring.EndDate))
            {
                var runDate = recurring.NextRunOn;

                if (existingRunSet.Contains((recurring.Id, runDate)))
                {
                    recurring.AdvanceNextRunOn();
                    continue;
                }

                var transaction = Transaction.Create(
                    recurring.TenantId,
                    recurring.AccountId,
                    recurring.CategoryId,
                    runDate,
                    recurring.Description,
                    recurring.Type,
                    recurring.AmountCents);

                _context.Set<Transaction>().Add(transaction);

                var run = RecurringTransactionRun.Create(
                    recurring.TenantId,
                    recurring.Id,
                    runDate,
                    transaction.Id);

                _context.Set<RecurringTransactionRun>().Add(run);

                existingRunSet.Add((recurring.Id, runDate));
                generatedTransactions++;

                recurring.AdvanceNextRunOn();
            }

            if (recurring.EndDate.HasValue && recurring.NextRunOn > recurring.EndDate.Value)
                recurring.Deactivate();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new RunRecurringTransactionsResponse(processedRules, generatedTransactions));
    }
}
