using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class AiSkillExecutionConfiguration : IEntityTypeConfiguration<AiSkillExecution>
{
    public void Configure(EntityTypeBuilder<AiSkillExecution> builder)
    {
        builder.Property(x => x.SkillId)
            .HasConversion(id => id.Value, value => new AiSkillId(value));
        builder.HasIndex(x => x.SkillId);
    }
}
