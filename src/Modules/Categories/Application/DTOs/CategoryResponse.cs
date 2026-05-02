namespace StaqFinance.Modules.Categories.Application.DTOs;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt);
