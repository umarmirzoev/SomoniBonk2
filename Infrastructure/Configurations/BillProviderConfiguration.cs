using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class BillProviderConfiguration : IEntityTypeConfiguration<BillProvider>
{
    public void Configure(EntityTypeBuilder<BillProvider> builder)
    {
        builder.ToTable("BillProviders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Providers)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Tcell", Code = "tcell", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Megafon TJ", Code = "megafon_tj", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Babilon Mobile", Code = "babilon_mobile", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Babilon-T", Code = "babilon_t", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "CJSC Telecom", Code = "cjsc_telecom", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000006"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "TojNET", Code = "tojnet", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000007"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Obi Khoja", Code = "obi_khoja", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000008"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Barqi Tojik", Code = "barqi_tojik", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000009"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Tojikgas", Code = "tojikgas", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000010"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Continent TV", Code = "continent_tv", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000011"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Sama TV", Code = "sama_tv", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000012"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Traffic fines", Code = "traffic_fines", IsActive = true },
            new BillProvider { Id = Guid.Parse("20000000-0000-0000-0000-000000000013"), CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Income tax", Code = "income_tax", IsActive = true }
        );
    }
}
