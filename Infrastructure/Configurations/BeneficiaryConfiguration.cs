using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> builder)
    {
        builder.ToTable("Beneficiaries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.BankName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.AccountNumber).HasMaxLength(34);
        builder.Property(x => x.CardNumber).HasMaxLength(20);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.TransferType).IsRequired().HasConversion<string>();
        builder.Property(x => x.Nickname).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Beneficiaries)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
