using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class AiAgentPerformanceMetricConfiguration : IEntityTypeConfiguration<AiAgentPerformanceMetric>
{
    public void Configure(EntityTypeBuilder<AiAgentPerformanceMetric> builder)
    {
        builder.Property(x => x.AgentId)
            .HasConversion(id => id.Value, value => new AiAgentId(value));
        builder.HasIndex(x => x.AgentId);
    }
}
