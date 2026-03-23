using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class AutoCreditConfiguration : IEntityTypeConfiguration<AutoCredit>
{
    public void Configure(EntityTypeBuilder<AutoCredit> builder)
    {
        builder.ToTable("AutoCredits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CarBrand).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CarModel).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CarYear).IsRequired();
        builder.Property(x => x.CarPrice).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.DownPayment).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.LoanAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.InterestRate).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.MonthlyPayment).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.RemainingAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.TermMonths).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasConversion<string>();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.AutoCredits)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.AutoCredits)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
