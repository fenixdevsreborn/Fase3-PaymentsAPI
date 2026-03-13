using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Payments.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.Provider).HasMaxLength(64);
        builder.Property(p => p.ProviderReference).HasMaxLength(256);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => new { p.UserId, p.CreatedAt });
        builder.HasIndex(p => p.ProviderReference).IsUnique(false);
    }
}
