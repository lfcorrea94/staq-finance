using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StaqFinance.Modules.Identity.Domain.Entities;
using StaqFinance.Modules.Tenancy.Domain.Entities;
using StaqFinance.Modules.Tenancy.Infrastructure.Persistence.Configurations;

namespace StaqFinance.Api.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new TenantConfiguration());
        builder.ApplyConfiguration(new TenantUserConfiguration());
    }
}
