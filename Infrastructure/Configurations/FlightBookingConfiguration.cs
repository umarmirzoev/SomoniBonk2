using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class FlightBookingConfiguration : IEntityTypeConfiguration<FlightBooking>
{
    public void Configure(EntityTypeBuilder<FlightBooking> builder)
    {
        builder.ToTable("FlightBookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FlightNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FromCity).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ToCity).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DepartureDate).IsRequired();
        builder.Property(x => x.PassengerCount).IsRequired();
        builder.Property(x => x.TotalPrice).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.MonthlyPayment).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Currency).IsRequired().HasConversion<string>();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.FlightBookings)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.FlightBookings)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
