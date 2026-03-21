namespace StaqFinance.Modules.Tenancy.Domain.Entities;

public sealed class TenantUser
{
    private TenantUser() { }

    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = "Owner";
    public DateTime JoinedAt { get; private set; }

    public static TenantUser Create(Guid tenantId, Guid userId, string role = "Owner")
    {
        return new TenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }
}
