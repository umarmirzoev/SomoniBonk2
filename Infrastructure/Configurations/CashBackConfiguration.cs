using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class CashbackConfiguration : IEntityTypeConfiguration<Cashback>
{
    public void Configure(EntityTypeBuilder<Cashback> builder)
    {
        builder.ToTable("Cashbacks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.Percentage).IsRequired().HasColumnType("decimal(5,2)");
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Cashbacks)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}