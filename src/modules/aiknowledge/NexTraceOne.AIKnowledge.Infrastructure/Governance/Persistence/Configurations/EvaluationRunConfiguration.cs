using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade EvaluationRun.
/// Tabela: aik_evaluation_runs.
/// </summary>
public sealed class EvaluationRunConfiguration : IEntityTypeConfiguration<EvaluationRun>
{
    public void Configure(EntityTypeBuilder<EvaluationRun> builder)
    {
        builder.ToTable("aik_evaluation_runs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => EvaluationRunId.From(value));

        builder.Property(e => e.SuiteId)
            .HasConversion(id => id.Value, value => EvaluationSuiteId.From(value))
            .IsRequired();

        builder.Property(e => e.ModelId).IsRequired();
        builder.Property(e => e.PromptVersion).HasMaxLength(100).IsRequired();

        builder.Property(e => e.Status)
            .HasConversion(v => v.ToString(), v => Enum.Parse<EvaluationRunStatus>(v))
            .HasMaxLength(50);

        builder.Property(e => e.StartedAt);
        builder.Property(e => e.CompletedAt);
        builder.Property(e => e.TotalCases);
        builder.Property(e => e.PassedCases);
        builder.Property(e => e.FailedCases);
        builder.Property(e => e.AverageLatencyMs);
        builder.Property(e => e.TotalTokenCost).HasColumnType("numeric(14,6)");
        builder.Property(e => e.TenantId).IsRequired();

        builder.Property(e => e.CreatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(500);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy).HasMaxLength(500);

        builder.HasIndex(e => e.SuiteId).HasDatabaseName("idx_aik_eval_runs_suite");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_aik_eval_runs_tenant");
    }
}
