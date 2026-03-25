using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="RuntimeBaseline"/>.</summary>
internal sealed class RuntimeBaselineConfiguration : IEntityTypeConfiguration<RuntimeBaseline>
{
    public void Configure(EntityTypeBuilder<RuntimeBaseline> builder)
    {
        builder.ToTable("ops_runtime_baselines");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RuntimeBaselineId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.ExpectedAvgLatencyMs).IsRequired();
        builder.Property(x => x.ExpectedP99LatencyMs).IsRequired();
        builder.Property(x => x.ExpectedErrorRate).IsRequired();
        builder.Property(x => x.ExpectedRequestsPerSecond).IsRequired();

        builder.Property(x => x.EstablishedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DataPointCount).IsRequired();
        builder.Property(x => x.ConfidenceScore).IsRequired();

        builder.HasIndex(x => new { x.ServiceName, x.Environment }).IsUnique();
        builder.HasIndex(x => x.EstablishedAt);
    }
}
