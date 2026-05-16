namespace StaqFinance.Modules.RecurringTransactions.Domain.Entities;

public sealed class RecurringTransactionRun
{
    private RecurringTransactionRun() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid RecurringTransactionId { get; private set; }
    public DateOnly RunDate { get; private set; }
    public Guid GeneratedTransactionId { get; private set; }

    public static RecurringTransactionRun Create(
        Guid tenantId,
        Guid recurringTransactionId,
        DateOnly runDate,
        Guid generatedTransactionId)
    {
        return new RecurringTransactionRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecurringTransactionId = recurringTransactionId,
            RunDate = runDate,
            GeneratedTransactionId = generatedTransactionId
        };
    }
}
