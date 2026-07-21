using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class AiAgentTrajectoryFeedbackConfiguration : IEntityTypeConfiguration<AiAgentTrajectoryFeedback>
{
    public void Configure(EntityTypeBuilder<AiAgentTrajectoryFeedback> builder)
    {
        builder.Property(x => x.ExecutionId)
            .HasConversion(id => id.Value, value => new AiAgentExecutionId(value));
        builder.HasIndex(x => x.ExecutionId);
    }
}
