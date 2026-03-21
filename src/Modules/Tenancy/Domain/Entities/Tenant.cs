namespace StaqFinance.Modules.Tenancy.Domain.Entities;

public sealed class Tenant
{
    private Tenant() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "BRL";
    public DateTime CreatedAt { get; private set; }

    public static Tenant Create(string name, string slug, string currency = "BRL")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Currency = currency,
            CreatedAt = DateTime.UtcNow
        };
    }
}
