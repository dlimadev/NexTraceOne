using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class EvaluationCaseConfiguration : IEntityTypeConfiguration<EvaluationCase>
{
    public void Configure(EntityTypeBuilder<EvaluationCase> builder)
    {
        builder.Property(x => x.SuiteId)
            .HasConversion(id => id.Value, value => new EvaluationSuiteId(value));
        builder.HasIndex(x => x.SuiteId);
    }
}
