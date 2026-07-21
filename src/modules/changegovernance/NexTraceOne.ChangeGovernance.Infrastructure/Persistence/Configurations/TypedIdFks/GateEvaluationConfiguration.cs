using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class GateEvaluationConfiguration : IEntityTypeConfiguration<GateEvaluation>
{
    public void Configure(EntityTypeBuilder<GateEvaluation> builder)
    {
        builder.Property(x => x.PromotionRequestId)
            .HasConversion(id => id.Value, value => new PromotionRequestId(value));
        builder.HasIndex(x => x.PromotionRequestId);
        builder.Property(x => x.PromotionGateId)
            .HasConversion(id => id.Value, value => new PromotionGateId(value));
        builder.HasIndex(x => x.PromotionGateId);
    }
}
