using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class QrPaymentConfiguration : IEntityTypeConfiguration<QrPayment>
{
    public void Configure(EntityTypeBuilder<QrPayment> builder)
    {
        builder.ToTable("QrPayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.Currency).IsRequired().HasConversion<string>();
        builder.Property(x => x.QrCode).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.QrCode).IsUnique();

        builder.HasOne(x => x.FromUser)
            .WithMany(x => x.GeneratedQrPayments)
            .HasForeignKey(x => x.FromUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ToAccount)
            .WithMany(x => x.ReceivedQrPayments)
            .HasForeignKey(x => x.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
