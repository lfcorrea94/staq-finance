namespace StaqFinance.Modules.Accounts.Domain.Entities;

public sealed class Account
{
    private Account() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static Account Create(Guid tenantId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Account
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }
}
