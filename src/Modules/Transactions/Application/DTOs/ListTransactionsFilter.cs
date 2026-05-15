using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Modules.Transactions.Application.DTOs;

public sealed record ListTransactionsFilter(
    DateOnly? From,
    DateOnly? To,
    Guid? AccountId,
    Guid? CategoryId,
    TransactionType? Type);
