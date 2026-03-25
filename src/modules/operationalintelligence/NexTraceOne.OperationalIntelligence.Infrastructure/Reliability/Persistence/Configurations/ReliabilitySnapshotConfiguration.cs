using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade ReliabilitySnapshot.</summary>
internal sealed class ReliabilitySnapshotConfiguration : IEntityTypeConfiguration<ReliabilitySnapshot>
{
    public void Configure(EntityTypeBuilder<ReliabilitySnapshot> builder)
    {
        builder.ToTable("ops_reliability_snapshots", t =>
        {
            t.HasCheckConstraint("CK_ops_reliability_snapshots_trend", "\"TrendDirection\" >= 0 AND \"TrendDirection\" <= 2");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReliabilitySnapshotId.From(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RuntimeHealthStatus).HasMaxLength(50).IsRequired();

        builder.Property(x => x.OverallScore).IsRequired();
        builder.Property(x => x.RuntimeHealthScore).IsRequired();
        builder.Property(x => x.IncidentImpactScore).IsRequired();
        builder.Property(x => x.ObservabilityScore).IsRequired();

        builder.Property(x => x.OpenIncidentCount).IsRequired();
        builder.Property(x => x.TrendDirection).HasColumnType("integer").IsRequired();
        builder.Property(x => x.ComputedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ServiceId, x.ComputedAt });
        builder.HasIndex(x => new { x.TenantId, x.ComputedAt });

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
