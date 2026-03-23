using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        builder.ToTable("KycDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasConversion<string>();
        builder.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.UploadedAt).IsRequired();

        builder.HasOne(x => x.KycProfile)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.KycProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
