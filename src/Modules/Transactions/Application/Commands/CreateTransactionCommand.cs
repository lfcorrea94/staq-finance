using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Transactions.Application.DTOs;
using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Modules.Transactions.Application.Commands;

public sealed record CreateTransactionCommand(
    Guid TenantId,
    Guid AccountId,
    Guid? CategoryId,
    DateOnly Date,
    string Description,
    TransactionType Type,
    long AmountCents);

public interface ICreateTransactionCommandHandler
{
    Task<Result<TransactionResponse>> HandleAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default);
}
