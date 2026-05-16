using StaqFinance.Modules.RecurringTransactions.Domain.Enums;
using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Modules.RecurringTransactions.Domain.Entities;

public sealed class RecurringTransaction
{
    private RecurringTransaction() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }
    public long AmountCents { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public RecurringFrequency Frequency { get; private set; }
    public int Interval { get; private set; }
    public DateOnly NextRunOn { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static RecurringTransaction Create(
        Guid tenantId,
        Guid accountId,
        Guid? categoryId,
        string description,
        TransactionType type,
        long amountCents,
        DateOnly startDate,
        DateOnly? endDate,
        RecurringFrequency frequency,
        int interval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new RecurringTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountId = accountId,
            CategoryId = categoryId,
            Description = description,
            Type = type,
            AmountCents = amountCents,
            StartDate = startDate,
            EndDate = endDate,
            Frequency = frequency,
            Interval = interval,
            NextRunOn = startDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public DateOnly AdvanceNextRunOn()
    {
        NextRunOn = Frequency switch
        {
            RecurringFrequency.Monthly => NextRunOn.AddMonths(Interval),
            RecurringFrequency.Weekly => NextRunOn.AddDays(7 * Interval),
            _ => throw new InvalidOperationException("Frequência desconhecida.")
        };
        return NextRunOn;
    }

    public void Deactivate() => IsActive = false;
}
