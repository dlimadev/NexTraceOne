using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a FK typed-id não descoberta pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class AnomalyNarrativeConfiguration : IEntityTypeConfiguration<AnomalyNarrative>
{
    public void Configure(EntityTypeBuilder<AnomalyNarrative> builder)
    {
        builder.Property(x => x.DriftFindingId)
            .HasConversion(id => id.Value, value => new DriftFindingId(value));
        builder.HasIndex(x => x.DriftFindingId);
    }
}
