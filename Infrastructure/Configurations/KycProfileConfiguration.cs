using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class KycProfileConfiguration : IEntityTypeConfiguration<KycProfile>
{
    public void Configure(EntityTypeBuilder<KycProfile> builder)
    {
        builder.ToTable("KycProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.NationalIdNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PassportNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(300);
        builder.Property(x => x.SelfieImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DocumentFrontUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DocumentBackUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.NationalIdNumber).IsUnique();

        builder.HasOne(x => x.User)
            .WithOne(x => x.KycProfile)
            .HasForeignKey<KycProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
