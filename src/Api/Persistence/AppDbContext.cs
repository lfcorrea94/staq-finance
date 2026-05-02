using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StaqFinance.Modules.Accounts.Domain.Entities;
using StaqFinance.Modules.Accounts.Infrastructure.Persistence.Configurations;
using StaqFinance.Modules.Categories.Domain.Entities;
using StaqFinance.Modules.Categories.Infrastructure.Persistence.Configurations;
using StaqFinance.Modules.Identity.Domain.Entities;
using StaqFinance.Modules.Identity.Infrastructure.Persistence.Configurations;
using StaqFinance.Modules.Tenancy.Application.Interfaces;
using StaqFinance.Modules.Tenancy.Domain.Entities;
using StaqFinance.Modules.Tenancy.Infrastructure.Persistence.Configurations;

namespace StaqFinance.Api.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    private readonly ICurrentTenant _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant) : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new TenantConfiguration());
        builder.ApplyConfiguration(new TenantUserConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        builder.ApplyConfiguration(new AccountConfiguration());
        builder.ApplyConfiguration(new CategoryConfiguration());

        builder.Entity<Account>()
            .HasQueryFilter(a => !_currentTenant.IsResolved || a.TenantId == _currentTenant.TenantId);

        builder.Entity<Category>()
            .HasQueryFilter(c => !_currentTenant.IsResolved || c.TenantId == _currentTenant.TenantId);
    }
}
