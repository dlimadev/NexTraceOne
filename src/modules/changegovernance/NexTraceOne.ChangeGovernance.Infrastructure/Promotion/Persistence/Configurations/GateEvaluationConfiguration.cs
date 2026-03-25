using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Configurations;

internal sealed class GateEvaluationConfiguration : IEntityTypeConfiguration<GateEvaluation>
{
    /// <summary>Configura o mapeamento da entidade GateEvaluation para a tabela prm_gate_evaluations.</summary>
    public void Configure(EntityTypeBuilder<GateEvaluation> builder)
    {
        builder.ToTable("chg_gate_evaluations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => GateEvaluationId.From(value));

        builder.Property(x => x.PromotionRequestId)
            .HasConversion(id => id.Value, value => PromotionRequestId.From(value))
            .IsRequired();
        builder.Property(x => x.PromotionGateId)
            .HasConversion(id => id.Value, value => PromotionGateId.From(value))
            .IsRequired();
        builder.Property(x => x.Passed).IsRequired();
        builder.Property(x => x.EvaluatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.EvaluationDetails).HasMaxLength(4000);
        builder.Property(x => x.OverrideJustification).HasMaxLength(4000);
        builder.Property(x => x.EvaluatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.PromotionRequestId);
        builder.HasIndex(x => x.PromotionGateId);
        builder.HasIndex(x => new { x.PromotionRequestId, x.PromotionGateId });
    }
}
