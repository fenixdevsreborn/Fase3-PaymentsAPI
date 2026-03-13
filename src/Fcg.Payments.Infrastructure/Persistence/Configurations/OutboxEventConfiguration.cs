using Fcg.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Payments.Infrastructure.Persistence.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EventName).HasMaxLength(128);
        builder.Property(e => e.Status).HasMaxLength(32);
        builder.HasIndex(e => new { e.Status, e.CreatedAt });
    }
}
