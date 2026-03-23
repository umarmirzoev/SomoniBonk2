using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class TransactionLimitConfiguration : IEntityTypeConfiguration<TransactionLimit>
{
    public void Configure(EntityTypeBuilder<TransactionLimit> builder)
    {
        builder.ToTable("TransactionLimits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DailyLimit).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.SingleTransactionLimit).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.UsedTodayAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.LastResetDate).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.Account)
            .WithOne(x => x.TransactionLimit)
            .HasForeignKey<TransactionLimit>(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}