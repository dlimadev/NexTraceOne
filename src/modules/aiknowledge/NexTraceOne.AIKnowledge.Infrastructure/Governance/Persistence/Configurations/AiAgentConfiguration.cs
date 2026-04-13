using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiAgent.
/// Tabela: aik_agents.
/// </summary>
public sealed class AiAgentConfiguration : IEntityTypeConfiguration<AiAgent>
{
    public void Configure(EntityTypeBuilder<AiAgent> builder)
    {
        builder.ToTable("aik_agents", t =>
        {
            t.HasCheckConstraint(
                "CK_aik_agents_Category",
                "\"Category\" IN ('General','ServiceAnalysis','ContractGovernance','IncidentResponse','ChangeIntelligence','SecurityAudit','FinOps','CodeReview','Documentation','Testing','Compliance','ApiDesign','TestGeneration','EventDesign','DocumentationAssistance','SoapDesign')");
            t.HasCheckConstraint(
                "CK_aik_agents_PublicationStatus",
                "\"PublicationStatus\" IN ('Draft','PendingReview','Active','Published','Archived','Blocked')");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => AiAgentId.From(value));

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Category)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AgentCategory>(v))
            .HasMaxLength(100);
        builder.Property(e => e.SystemPrompt).HasMaxLength(10000);
        builder.Property(e => e.Capabilities).HasMaxLength(1000);
        builder.Property(e => e.TargetPersona).HasMaxLength(200);
        builder.Property(e => e.Icon).HasMaxLength(100);

        // Phase 3: Agent Runtime Foundation
        builder.Property(e => e.OwnershipType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AgentOwnershipType>(v))
            .HasMaxLength(50);
        builder.Property(e => e.Visibility)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AgentVisibility>(v))
            .HasMaxLength(50);
        builder.Property(e => e.PublicationStatus)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AgentPublicationStatus>(v))
            .HasMaxLength(50);
        builder.Property(e => e.OwnerId).HasMaxLength(200);
        builder.Property(e => e.OwnerTeamId).HasMaxLength(200);
        builder.Property(e => e.AllowedModelIds).HasMaxLength(2000);
        builder.Property(e => e.AllowedTools).HasMaxLength(2000);
        builder.Property(e => e.Objective).HasMaxLength(2000);
        builder.Property(e => e.InputSchema).HasMaxLength(5000);
        builder.Property(e => e.OutputSchema).HasMaxLength(5000);
        builder.Property(e => e.UsePlanningMode).HasDefaultValue(false);

        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.OwnershipType);
        builder.HasIndex(e => e.OwnerId);
        builder.HasIndex(e => e.PublicationStatus);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
