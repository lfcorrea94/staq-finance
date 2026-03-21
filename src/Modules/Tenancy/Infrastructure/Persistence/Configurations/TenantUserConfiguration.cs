using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StaqFinance.Modules.Tenancy.Domain.Entities;

namespace StaqFinance.Modules.Tenancy.Infrastructure.Persistence.Configurations;

public sealed class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("TenantUsers");

        builder.HasKey(tu => new { tu.TenantId, tu.UserId });

        builder.Property(tu => tu.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(tu => tu.JoinedAt)
            .IsRequired();

        builder.HasIndex(tu => tu.UserId);
    }
}
