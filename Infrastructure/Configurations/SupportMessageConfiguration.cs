using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.ToTable("SupportMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MessageText).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.SentAt).IsRequired();
        builder.Property(x => x.IsRead).IsRequired();

        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SenderUser)
            .WithMany(x => x.SupportMessages)
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SenderAdmin)
            .WithMany()
            .HasForeignKey(x => x.SenderAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
