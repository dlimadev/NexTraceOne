using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ObservabilityProfile"/>.</summary>
internal sealed class ObservabilityProfileConfiguration : IEntityTypeConfiguration<ObservabilityProfile>
{
    public void Configure(EntityTypeBuilder<ObservabilityProfile> builder)
    {
        builder.ToTable("oi_observability_profiles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ObservabilityProfileId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.HasTracing).IsRequired();
        builder.Property(x => x.HasMetrics).IsRequired();
        builder.Property(x => x.HasLogging).IsRequired();
        builder.Property(x => x.HasAlerting).IsRequired();
        builder.Property(x => x.HasDashboard).IsRequired();

        builder.Property(x => x.ObservabilityScore).IsRequired();
        builder.Property(x => x.LastAssessedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.ServiceName, x.Environment }).IsUnique();
        builder.HasIndex(x => x.LastAssessedAt);
    }
}
