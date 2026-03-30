using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiEvaluation.
/// Tabela: aik_evaluations.
/// </summary>
internal sealed class AiEvaluationConfiguration : IEntityTypeConfiguration<AiEvaluation>
{
    public void Configure(EntityTypeBuilder<AiEvaluation> builder)
    {
        builder.ToTable("aik_evaluations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiEvaluationId.From(value));

        builder.Property(x => x.EvaluationType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.PromptTemplateName).HasMaxLength(200);
        builder.Property(x => x.RelevanceScore).HasColumnType("numeric(5,4)").IsRequired();
        builder.Property(x => x.AccuracyScore).HasColumnType("numeric(5,4)").IsRequired();
        builder.Property(x => x.UsefulnessScore).HasColumnType("numeric(5,4)").IsRequired();
        builder.Property(x => x.SafetyScore).HasColumnType("numeric(5,4)").IsRequired();
        builder.Property(x => x.OverallScore).HasColumnType("numeric(5,4)").IsRequired();
        builder.Property(x => x.Feedback).HasMaxLength(5000);
        builder.Property(x => x.Tags).HasMaxLength(500);
        builder.Property(x => x.EvaluatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.AgentExecutionId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EvaluatedAt);
        builder.HasIndex(x => x.OverallScore);
    }
}
