using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class VirtualCardConfiguration : IEntityTypeConfiguration<VirtualCard>
{
    public void Configure(EntityTypeBuilder<VirtualCard> builder)
    {
        builder.ToTable("VirtualCards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CardHolderName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.MaskedCardNumber).IsRequired().HasMaxLength(32);
        builder.Property(x => x.CvvHash).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DailyLimit).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.MonthlyLimit).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.VirtualCards)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LinkedAccount)
            .WithMany(x => x.VirtualCards)
            .HasForeignKey(x => x.LinkedAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
