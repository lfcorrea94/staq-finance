using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StaqFinance.Modules.RecurringTransactions.Domain.Entities;

namespace StaqFinance.Modules.RecurringTransactions.Infrastructure.Persistence.Configurations;

public sealed class RecurringTransactionConfiguration : IEntityTypeConfiguration<RecurringTransaction>
{
    public void Configure(EntityTypeBuilder<RecurringTransaction> builder)
    {
        builder.ToTable("RecurringTransactions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.HasIndex(r => r.TenantId);

        builder.Property(r => r.AccountId)
            .IsRequired();

        builder.HasOne(typeof(StaqFinance.Modules.Accounts.Domain.Entities.Account))
            .WithMany()
            .HasForeignKey(nameof(RecurringTransaction.AccountId))
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(typeof(StaqFinance.Modules.Categories.Domain.Entities.Category))
            .WithMany()
            .HasForeignKey(nameof(RecurringTransaction.CategoryId))
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(r => r.CategoryId)
            .IsRequired(false);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(r => r.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.AmountCents)
            .IsRequired();

        builder.Property(r => r.StartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(r => r.EndDate)
            .IsRequired(false)
            .HasColumnType("date");

        builder.Property(r => r.Frequency)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Interval)
            .IsRequired();

        builder.Property(r => r.NextRunOn)
            .IsRequired()
            .HasColumnType("date");

        builder.HasIndex(r => r.NextRunOn);

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();
    }
}
