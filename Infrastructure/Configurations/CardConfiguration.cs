using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CardNumber).IsRequired().HasMaxLength(16);
        builder.Property(x => x.CardHolderName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExpiryDate).IsRequired().HasMaxLength(5);
        builder.Property(x => x.Cvv).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.CardNumber).IsUnique();
    }
}