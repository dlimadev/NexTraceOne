using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="DriftFinding"/>.</summary>
internal sealed class DriftFindingConfiguration : IEntityTypeConfiguration<DriftFinding>
{
    public void Configure(EntityTypeBuilder<DriftFinding> builder)
    {
        builder.ToTable("oi_drift_findings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DriftFindingId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MetricName).HasMaxLength(200).IsRequired();

        builder.Property(x => x.ExpectedValue).IsRequired();
        builder.Property(x => x.ActualValue).IsRequired();
        builder.Property(x => x.DeviationPercent).IsRequired();
        builder.Property(x => x.Severity).HasColumnType("integer").IsRequired();

        builder.Property(x => x.DetectedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReleaseId);

        builder.Property(x => x.IsAcknowledged).IsRequired();
        builder.Property(x => x.IsResolved).IsRequired();
        builder.Property(x => x.ResolutionComment).HasMaxLength(2000);
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.ServiceName, x.Environment, x.DetectedAt });
        builder.HasIndex(x => x.Severity);
        builder.HasIndex(x => x.IsResolved);
    }
}
