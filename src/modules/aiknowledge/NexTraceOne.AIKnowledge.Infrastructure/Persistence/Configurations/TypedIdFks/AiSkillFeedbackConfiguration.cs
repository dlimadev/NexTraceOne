using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class AiSkillFeedbackConfiguration : IEntityTypeConfiguration<AiSkillFeedback>
{
    public void Configure(EntityTypeBuilder<AiSkillFeedback> builder)
    {
        builder.Property(x => x.SkillExecutionId)
            .HasConversion(id => id.Value, value => new AiSkillExecutionId(value));
        builder.HasIndex(x => x.SkillExecutionId);
    }
}
