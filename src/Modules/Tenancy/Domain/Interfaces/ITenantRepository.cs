using StaqFinance.Modules.Tenancy.Domain.Entities;

namespace StaqFinance.Modules.Tenancy.Domain.Interfaces;

public interface ITenantRepository
{
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsMemberAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}
