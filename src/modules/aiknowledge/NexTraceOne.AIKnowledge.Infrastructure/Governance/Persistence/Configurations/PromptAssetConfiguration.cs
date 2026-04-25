using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade PromptAsset.
/// Tabela: aik_prompt_assets.
/// </summary>
public sealed class PromptAssetConfiguration : IEntityTypeConfiguration<PromptAsset>
{
    public void Configure(EntityTypeBuilder<PromptAsset> builder)
    {
        builder.ToTable("aik_prompt_assets");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => PromptAssetId.From(value));

        builder.Property(e => e.Slug).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Tags).HasMaxLength(500);
        builder.Property(e => e.Variables).HasMaxLength(500);
        builder.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(e => e.CurrentVersionNumber).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();

        builder.HasIndex(e => new { e.Slug, e.TenantId }).IsUnique();

        builder.HasMany(e => e.Versions)
            .WithOne()
            .HasForeignKey(v => v.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
