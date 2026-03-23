using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class BillCategoryConfiguration : IEntityTypeConfiguration<BillCategory>
{
    public void Configure(EntityTypeBuilder<BillCategory> builder)
    {
        builder.ToTable("BillCategories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new BillCategory { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Mobile operators", Code = "mobile", IsActive = true },
            new BillCategory { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Internet providers", Code = "internet", IsActive = true },
            new BillCategory { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Utilities", Code = "utilities", IsActive = true },
            new BillCategory { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "TV", Code = "tv", IsActive = true },
            new BillCategory { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Fines and taxes", Code = "fines_taxes", IsActive = true }
        );
    }
}
