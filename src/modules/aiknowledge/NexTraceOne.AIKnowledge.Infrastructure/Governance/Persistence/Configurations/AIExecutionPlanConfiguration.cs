using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIExecutionPlanConfiguration : IEntityTypeConfiguration<AIExecutionPlan>
{
    public void Configure(EntityTypeBuilder<AIExecutionPlan> builder)
    {
        builder.ToTable("aik_execution_plans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIExecutionPlanId.From(value));

        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.InputQuery).HasMaxLength(10_000).IsRequired();
        builder.Property(x => x.Persona).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UseCaseType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.SelectedModel).HasMaxLength(300).IsRequired();
        builder.Property(x => x.SelectedProvider).HasMaxLength(300).IsRequired();
        builder.Property(x => x.RoutingPath).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.SelectedSources).HasMaxLength(2000);
        builder.Property(x => x.SourceWeightingSummary).HasMaxLength(2000);
        builder.Property(x => x.PolicyDecision).HasMaxLength(500);
        builder.Property(x => x.EstimatedCostClass).HasMaxLength(50);
        builder.Property(x => x.RationaleSummary).HasMaxLength(4000);
        builder.Property(x => x.ConfidenceLevel).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.EscalationReason).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.PlannedAt).HasColumnType("timestamp with time zone").IsRequired();

        // E-A04: FK para AiAgentExecution
        builder.Property(x => x.ExecutionId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : AiAgentExecutionId.From(value.Value))
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.PlannedAt);
        builder.HasIndex(x => x.ExecutionId);
    }
}
