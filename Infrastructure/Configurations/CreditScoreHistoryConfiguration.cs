using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class CreditScoreHistoryConfiguration : IEntityTypeConfiguration<CreditScoreHistory>
{
    public void Configure(EntityTypeBuilder<CreditScoreHistory> builder)
    {
        builder.ToTable("CreditScoreHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MonthlyIncome).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.EmploymentStatus).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExistingDebt).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.AccountTurnover).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.ScoreValue).IsRequired();
        builder.Property(x => x.Decision).IsRequired().HasConversion<string>();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CalculatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.CreditScoreHistory)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
