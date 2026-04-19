using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiSkillExecution.
/// Tabela: aik_skill_executions.
/// </summary>
public sealed class AiSkillExecutionConfiguration : IEntityTypeConfiguration<AiSkillExecution>
{
    public void Configure(EntityTypeBuilder<AiSkillExecution> builder)
    {
        builder.ToTable("aik_skill_executions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => AiSkillExecutionId.From(value));

        builder.Property(e => e.SkillId)
            .HasConversion(
                id => id.Value,
                value => AiSkillId.From(value))
            .IsRequired();

        builder.Property(e => e.AgentId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : AiAgentId.From(value.Value));

        builder.Property(e => e.ExecutedBy).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ModelUsed).HasMaxLength(200);
        builder.Property(e => e.InputJson).HasMaxLength(32_000);
        builder.Property(e => e.OutputJson).HasMaxLength(64_000);
        builder.Property(e => e.ErrorMessage).HasMaxLength(4000);

        builder.HasIndex(e => e.SkillId);
        builder.HasIndex(e => e.ExecutedBy);
        builder.HasIndex(e => e.ExecutedAt);
    }
}
