using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiAgentTrajectoryFeedback.
/// Tabela: aik_agent_trajectory_feedbacks.
/// </summary>
public sealed class AiAgentTrajectoryFeedbackConfiguration : IEntityTypeConfiguration<AiAgentTrajectoryFeedback>
{
    public void Configure(EntityTypeBuilder<AiAgentTrajectoryFeedback> builder)
    {
        builder.ToTable("aik_agent_trajectory_feedbacks");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => AiAgentTrajectoryFeedbackId.From(value));

        builder.Property(e => e.ExecutionId)
            .HasConversion(
                id => id.Value,
                value => AiAgentExecutionId.From(value))
            .IsRequired();

        builder.Property(e => e.Outcome).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Comment).HasMaxLength(2000);
        builder.Property(e => e.ActualOutcome).HasMaxLength(2000);
        builder.Property(e => e.SubmittedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(e => e.ExecutionId);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ExportedForTraining);
    }
}
