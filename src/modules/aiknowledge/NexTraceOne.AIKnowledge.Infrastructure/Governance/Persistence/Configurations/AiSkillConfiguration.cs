using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiSkill.
/// Tabela: aik_skills.
/// </summary>
public sealed class AiSkillConfiguration : IEntityTypeConfiguration<AiSkill>
{
    public void Configure(EntityTypeBuilder<AiSkill> builder)
    {
        builder.ToTable("aik_skills");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => AiSkillId.From(value));

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.SkillContent).HasColumnType("text");
        builder.Property(e => e.Version).HasMaxLength(50).IsRequired();

        builder.Property(e => e.OwnershipType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<SkillOwnershipType>(v))
            .HasMaxLength(50);

        builder.Property(e => e.Visibility)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<SkillVisibility>(v))
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<SkillStatus>(v))
            .HasMaxLength(50);

        builder.Property(e => e.Tags).HasMaxLength(2000);
        builder.Property(e => e.RequiredTools).HasMaxLength(2000);
        builder.Property(e => e.PreferredModels).HasMaxLength(2000);
        builder.Property(e => e.InputSchema).HasMaxLength(16000);
        builder.Property(e => e.OutputSchema).HasMaxLength(16000);
        builder.Property(e => e.ParentAgentId).HasMaxLength(200);
        builder.Property(e => e.OwnerId).HasMaxLength(200);
        builder.Property(e => e.OwnerTeamId).HasMaxLength(200);

        builder.HasIndex(e => new { e.Name, e.TenantId });
        builder.HasIndex(e => e.OwnershipType);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
