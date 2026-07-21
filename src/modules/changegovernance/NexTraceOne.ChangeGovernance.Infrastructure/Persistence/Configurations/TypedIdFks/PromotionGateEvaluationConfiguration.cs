using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class PromotionGateEvaluationConfiguration : IEntityTypeConfiguration<PromotionGateEvaluation>
{
    public void Configure(EntityTypeBuilder<PromotionGateEvaluation> builder)
    {
        builder.Property(x => x.GateId)
            .HasConversion(id => id.Value, value => new PromotionGateId(value));
        builder.HasIndex(x => x.GateId);
    }
}
