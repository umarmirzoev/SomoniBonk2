using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class CurrencyRateConfiguration : IEntityTypeConfiguration<CurrencyRate>
{
    public void Configure(EntityTypeBuilder<CurrencyRate> builder)
    {
        builder.ToTable("CurrencyRates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromCurrency).IsRequired().HasConversion<string>();
        builder.Property(x => x.ToCurrency).IsRequired().HasConversion<string>();
        builder.Property(x => x.Rate).IsRequired().HasColumnType("decimal(18,6)");
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}