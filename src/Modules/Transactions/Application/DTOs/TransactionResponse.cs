using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Modules.Transactions.Application.DTOs;

public sealed record TransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid? CategoryId,
    DateOnly Date,
    string Description,
    TransactionType Type,
    long AmountCents,
    DateTime CreatedAt);
