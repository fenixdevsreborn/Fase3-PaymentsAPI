using Fcg.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Payments.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AggregateType).HasMaxLength(64);
        builder.Property(a => a.AggregateId).HasMaxLength(64);
        builder.Property(a => a.Action).HasMaxLength(64);
        builder.Property(a => a.TraceId).HasMaxLength(64);
        builder.Property(a => a.CorrelationId).HasMaxLength(64);
        builder.HasIndex(a => new { a.AggregateType, a.AggregateId });
    }
}
