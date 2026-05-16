using StaqFinance.Modules.RecurringTransactions.Domain.Enums;
using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Modules.RecurringTransactions.Application.DTOs;

public sealed record RecurringTransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid? CategoryId,
    string Description,
    TransactionType Type,
    long AmountCents,
    DateOnly StartDate,
    DateOnly? EndDate,
    RecurringFrequency Frequency,
    int Interval,
    DateOnly NextRunOn,
    bool IsActive,
    DateTime CreatedAt);
