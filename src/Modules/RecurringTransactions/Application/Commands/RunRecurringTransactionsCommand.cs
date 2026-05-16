using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.RecurringTransactions.Application.DTOs;

namespace StaqFinance.Modules.RecurringTransactions.Application.Commands;

public sealed record RunRecurringTransactionsCommand(Guid TenantId, DateOnly Until);

public interface IRunRecurringTransactionsCommandHandler
{
    Task<Result<RunRecurringTransactionsResponse>> HandleAsync(RunRecurringTransactionsCommand command, CancellationToken cancellationToken = default);
}
