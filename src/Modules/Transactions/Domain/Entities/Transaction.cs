using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Modules.Transactions.Domain.Entities;

public sealed class Transaction
{
    private Transaction() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }
    public long AmountCents { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Transaction Create(
        Guid tenantId,
        Guid accountId,
        Guid? categoryId,
        DateOnly date,
        string description,
        TransactionType type,
        long amountCents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountId = accountId,
            CategoryId = categoryId,
            Date = date,
            Description = description,
            Type = type,
            AmountCents = amountCents,
            CreatedAt = DateTime.UtcNow
        };
    }
}
