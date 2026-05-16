using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.RecurringTransactions.Application.DTOs;
using StaqFinance.Modules.RecurringTransactions.Domain.Enums;
using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Modules.RecurringTransactions.Application.Commands;

public sealed record CreateRecurringTransactionCommand(
    Guid TenantId,
    Guid AccountId,
    Guid? CategoryId,
    string Description,
    TransactionType Type,
    long AmountCents,
    DateOnly StartDate,
    DateOnly? EndDate,
    RecurringFrequency Frequency,
    int Interval);

public interface ICreateRecurringTransactionCommandHandler
{
    Task<Result<RecurringTransactionResponse>> HandleAsync(CreateRecurringTransactionCommand command, CancellationToken cancellationToken = default);
}
