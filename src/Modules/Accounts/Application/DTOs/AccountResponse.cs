namespace StaqFinance.Modules.Accounts.Application.DTOs;

public sealed record AccountResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt);
