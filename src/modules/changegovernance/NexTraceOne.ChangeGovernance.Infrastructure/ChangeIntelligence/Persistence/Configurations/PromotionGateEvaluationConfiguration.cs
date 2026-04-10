using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configura o mapeamento da entidade PromotionGateEvaluation para a tabela chg_promotion_gate_evaluations.</summary>
internal sealed class PromotionGateEvaluationConfiguration : IEntityTypeConfiguration<PromotionGateEvaluation>
{
    public void Configure(EntityTypeBuilder<PromotionGateEvaluation> builder)
    {
        builder.ToTable("chg_promotion_gate_evaluations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PromotionGateEvaluationId.From(value));

        builder.Property(x => x.GateId)
            .HasConversion(id => id.Value, value => PromotionGateId.From(value))
            .IsRequired();

        builder.Property(x => x.ChangeId).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Result)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RuleResults).HasColumnType("jsonb");
        builder.Property(x => x.EvaluatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EvaluatedBy).HasMaxLength(200);
        builder.Property(x => x.TenantId).HasMaxLength(200);
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.GateId);
        builder.HasIndex(x => x.ChangeId);
        builder.HasIndex(x => x.EvaluatedAt);
        builder.HasIndex(x => x.TenantId);
    }
}
