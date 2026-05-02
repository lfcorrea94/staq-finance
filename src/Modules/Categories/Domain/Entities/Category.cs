namespace StaqFinance.Modules.Categories.Domain.Entities;

public sealed class Category
{
    private Category() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static Category Create(Guid tenantId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Category
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }
}
