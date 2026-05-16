using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StaqFinance.Modules.RecurringTransactions.Domain.Entities;
using StaqFinance.Modules.Transactions.Domain.Entities;

namespace StaqFinance.Modules.RecurringTransactions.Infrastructure.Persistence.Configurations;

public sealed class RecurringTransactionRunConfiguration : IEntityTypeConfiguration<RecurringTransactionRun>
{
    public void Configure(EntityTypeBuilder<RecurringTransactionRun> builder)
    {
        builder.ToTable("RecurringTransactionRuns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.HasIndex(r => r.TenantId);

        builder.Property(r => r.RecurringTransactionId)
            .IsRequired();

        builder.HasOne(typeof(RecurringTransaction))
            .WithMany()
            .HasForeignKey(nameof(RecurringTransactionRun.RecurringTransactionId))
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.RunDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(r => r.GeneratedTransactionId)
            .IsRequired();

        builder.HasOne(typeof(Transaction))
            .WithMany()
            .HasForeignKey(nameof(RecurringTransactionRun.GeneratedTransactionId))
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.RecurringTransactionId, r.RunDate })
            .IsUnique();
    }
}
