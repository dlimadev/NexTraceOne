using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade PromptVersion.
/// Tabela: aik_prompt_versions.
/// </summary>
public sealed class PromptVersionConfiguration : IEntityTypeConfiguration<PromptVersion>
{
    public void Configure(EntityTypeBuilder<PromptVersion> builder)
    {
        builder.ToTable("aik_prompt_versions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => PromptVersionId.From(value));

        builder.Property(e => e.AssetId)
            .HasConversion(
                id => id.Value,
                value => PromptAssetId.From(value))
            .IsRequired();

        builder.Property(e => e.VersionNumber).IsRequired();
        builder.Property(e => e.Content).HasColumnType("text").IsRequired();
        builder.Property(e => e.ChangeNotes).HasMaxLength(1000);
        builder.Property(e => e.EvalScore).HasPrecision(5, 4);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(e => new { e.AssetId, e.VersionNumber }).IsUnique();
    }
}
