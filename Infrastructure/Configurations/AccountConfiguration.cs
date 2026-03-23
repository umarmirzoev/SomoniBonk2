using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SomoniBank.Domain.Models;

namespace SomoniBank.Infrastructure.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Type).IsRequired().HasConversion<string>();
        builder.Property(x => x.Currency).IsRequired().HasConversion<string>();
        builder.Property(x => x.Balance).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.AccountNumber).IsUnique();

        builder.HasMany(x => x.SentTransactions)
            .WithOne(x => x.FromAccount)
            .HasForeignKey(x => x.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ReceivedTransactions)
            .WithOne(x => x.ToAccount)
            .HasForeignKey(x => x.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Cards)
            .WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Deposits)
            .WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}