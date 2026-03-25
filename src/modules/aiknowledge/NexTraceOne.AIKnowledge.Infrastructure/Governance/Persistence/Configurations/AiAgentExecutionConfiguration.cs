using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiAgentExecution.
/// Tabela: aik_agent_executions.
/// </summary>
public sealed class AiAgentExecutionConfiguration : IEntityTypeConfiguration<AiAgentExecution>
{
    public void Configure(EntityTypeBuilder<AiAgentExecution> builder)
    {
        builder.ToTable("aik_agent_executions", t =>
        {
            t.HasCheckConstraint(
                "CK_aik_agent_executions_Status",
                "\"Status\" IN ('Pending','Running','Completed','Failed','Cancelled')");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => AiAgentExecutionId.From(value));

        builder.Property(e => e.AgentId)
            .HasConversion(
                id => id.Value,
                value => AiAgentId.From(value))
            .IsRequired();

        builder.Property(e => e.ExecutedBy).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AgentExecutionStatus>(v))
            .HasMaxLength(50);
        builder.Property(e => e.ProviderUsed).HasMaxLength(200);
        builder.Property(e => e.InputJson).HasMaxLength(32_000);
        builder.Property(e => e.OutputJson).HasMaxLength(64_000);
        builder.Property(e => e.CorrelationId).HasMaxLength(200);
        builder.Property(e => e.ErrorMessage).HasMaxLength(4000);
        builder.Property(e => e.Steps).HasMaxLength(32_000);
        builder.Property(e => e.ContextJson).HasMaxLength(16_000);

        builder.HasIndex(e => e.AgentId);
        builder.HasIndex(e => e.ExecutedBy);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CorrelationId);
        builder.HasIndex(e => e.StartedAt);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
