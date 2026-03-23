using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class InternationalTransferConfiguration : IEntityTypeConfiguration<InternationalTransfer>
{
    public void Configure(EntityTypeBuilder<InternationalTransfer> builder)
    {
        builder.ToTable("InternationalTransfers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecipientName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.RecipientBank).IsRequired().HasMaxLength(150);
        builder.Property(x => x.RecipientAccount).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Country).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.Currency).IsRequired().HasConversion<string>();
        builder.Property(x => x.ExchangeRate).IsRequired().HasColumnType("decimal(18,6)");
        builder.Property(x => x.AmountInTJS).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.Fee).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.InternationalTransfers)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromAccount)
            .WithMany(x => x.InternationalTransfers)
            .HasForeignKey(x => x.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
