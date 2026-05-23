using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Configurations;

internal sealed class AgentWorkflowExecutionConfiguration : IEntityTypeConfiguration<AgentWorkflowExecution>
{
    public void Configure(EntityTypeBuilder<AgentWorkflowExecution> builder)
    {
        builder.ToTable("aik_orch_workflow_executions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AgentWorkflowExecutionId.From(value));

        builder.Property(x => x.WorkflowName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.InitialInput).HasMaxLength(50000).IsRequired();
        builder.Property(x => x.FinalOutput).HasMaxLength(50000).IsRequired();
        builder.Property(x => x.StepResultsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TotalSteps).IsRequired();
        builder.Property(x => x.SuccessfulSteps).IsRequired();
        builder.Property(x => x.TotalRetries).IsRequired();
        builder.Property(x => x.DurationMs).IsRequired();
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CallerTeamId).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(5000);

        builder.HasIndex(x => x.WorkflowName);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.CallerTeamId);
        builder.HasIndex(x => x.StartedAt);
    }
}
