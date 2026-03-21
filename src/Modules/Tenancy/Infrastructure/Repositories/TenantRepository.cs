using Microsoft.EntityFrameworkCore;
using StaqFinance.Modules.Tenancy.Domain.Entities;
using StaqFinance.Modules.Tenancy.Domain.Interfaces;

namespace StaqFinance.Modules.Tenancy.Infrastructure.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly DbContext _context;

    public TenantRepository(DbContext context)
    {
        _context = context;
    }

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _context.Set<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public Task<bool> ExistsMemberAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Set<TenantUser>()
            .AsNoTracking()
            .AnyAsync(tu => tu.TenantId == tenantId && tu.UserId == userId, cancellationToken);
    }
}
