using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiSkillFeedback.
/// Tabela: aik_skill_feedbacks.
/// </summary>
public sealed class AiSkillFeedbackConfiguration : IEntityTypeConfiguration<AiSkillFeedback>
{
    public void Configure(EntityTypeBuilder<AiSkillFeedback> builder)
    {
        builder.ToTable("aik_skill_feedbacks");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => AiSkillFeedbackId.From(value));

        builder.Property(e => e.SkillExecutionId)
            .HasConversion(
                id => id.Value,
                value => AiSkillExecutionId.From(value))
            .IsRequired();

        builder.Property(e => e.Outcome).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Comment).HasMaxLength(2000);
        builder.Property(e => e.ActualOutcome).HasMaxLength(2000);
        builder.Property(e => e.SubmittedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(e => e.SkillExecutionId);
        builder.HasIndex(e => e.TenantId);
    }
}
