using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class RecurringPaymentHistoryConfiguration : IEntityTypeConfiguration<RecurringPaymentHistory>
{
    public void Configure(EntityTypeBuilder<RecurringPaymentHistory> builder)
    {
        builder.ToTable("RecurringPaymentHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.Message).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ExecutedAt).IsRequired();

        builder.HasOne(x => x.RecurringPayment)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.RecurringPaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Transaction)
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
