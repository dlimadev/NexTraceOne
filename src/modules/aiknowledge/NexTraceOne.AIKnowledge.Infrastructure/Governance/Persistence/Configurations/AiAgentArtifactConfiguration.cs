using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiAgentArtifact.
/// Tabela: aik_agent_artifacts.
/// </summary>
public sealed class AiAgentArtifactConfiguration : IEntityTypeConfiguration<AiAgentArtifact>
{
    public void Configure(EntityTypeBuilder<AiAgentArtifact> builder)
    {
        builder.ToTable("aik_agent_artifacts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => AiAgentArtifactId.From(value));

        builder.Property(e => e.ExecutionId)
            .HasConversion(
                id => id.Value,
                value => AiAgentExecutionId.From(value))
            .IsRequired();

        builder.Property(e => e.AgentId)
            .HasConversion(
                id => id.Value,
                value => AiAgentId.From(value))
            .IsRequired();

        builder.Property(e => e.ArtifactType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AgentArtifactType>(v))
            .HasMaxLength(50);

        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Format).HasMaxLength(50);

        builder.Property(e => e.ReviewStatus)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ArtifactReviewStatus>(v))
            .HasMaxLength(50);

        builder.Property(e => e.ReviewedBy).HasMaxLength(200);
        builder.Property(e => e.ReviewNotes).HasMaxLength(2000);

        builder.HasIndex(e => e.ExecutionId);
        builder.HasIndex(e => e.AgentId);
        builder.HasIndex(e => e.ReviewStatus);
        builder.HasIndex(e => e.ArtifactType);
    }
}
