using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// EF Core configuration for AiEvalRun. Table: aik_eval_runs.
/// CC-05: AI Evaluation Harness — model evaluation runs with aggregated metrics.
/// </summary>
public sealed class AiEvalRunConfiguration : IEntityTypeConfiguration<AiEvalRun>
{
    public void Configure(EntityTypeBuilder<AiEvalRun> builder)
    {
        builder.ToTable("aik_eval_runs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new AiEvalRunId(v));

        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DatasetId).IsRequired();
        builder.Property(e => e.ModelId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status)
            .HasConversion(v => v.ToString(), v => Enum.Parse<AiEvalRunStatus>(v))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(e => e.CasesProcessed).IsRequired();
        builder.Property(e => e.ExactMatchCount).IsRequired();
        builder.Property(e => e.AverageSemanticSimilarity).HasPrecision(5, 4).IsRequired();
        builder.Property(e => e.ToolCallAccuracy).HasPrecision(5, 4).IsRequired();
        builder.Property(e => e.LatencyP50Ms).IsRequired();
        builder.Property(e => e.LatencyP95Ms).IsRequired();
        builder.Property(e => e.TotalTokenCost).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.CompletedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(e => new { e.TenantId, e.DatasetId })
            .HasDatabaseName("ix_aik_eval_runs_tenant_dataset");
        builder.HasIndex(e => new { e.TenantId, e.ModelId })
            .HasDatabaseName("ix_aik_eval_runs_tenant_model");
    }
}
