using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StaqFinance.Modules.Transactions.Domain.Entities;

namespace StaqFinance.Modules.Transactions.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.TenantId)
            .IsRequired();

        builder.HasIndex(t => t.TenantId);

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.HasOne(typeof(StaqFinance.Modules.Accounts.Domain.Entities.Account))
            .WithMany()
            .HasForeignKey(nameof(Transaction.AccountId))
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(typeof(StaqFinance.Modules.Categories.Domain.Entities.Category))
            .WithMany()
            .HasForeignKey(nameof(Transaction.CategoryId))
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(t => t.CategoryId)
            .IsRequired(false);

        builder.Property(t => t.Date)
            .IsRequired()
            .HasColumnType("date");

        builder.HasIndex(t => t.Date);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.AmountCents)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
