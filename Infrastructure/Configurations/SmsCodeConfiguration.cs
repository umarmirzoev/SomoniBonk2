using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class SmsCodeConfiguration : IEntityTypeConfiguration<SmsCode>
{
    public void Configure(EntityTypeBuilder<SmsCode> builder)
    {
        builder.ToTable("SmsCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(10);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.IsUsed).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.Phone, x.CreatedAt });
    }
}
