namespace StaqFinance.Modules.RecurringTransactions.Application.DTOs;

public sealed record RunRecurringTransactionsResponse(
    int ProcessedRules,
    int GeneratedTransactions);
