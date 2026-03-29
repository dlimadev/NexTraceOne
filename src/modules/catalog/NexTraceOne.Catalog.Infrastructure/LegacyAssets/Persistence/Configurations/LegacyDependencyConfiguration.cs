using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class LegacyDependencyConfiguration : IEntityTypeConfiguration<LegacyDependency>
{
    public void Configure(EntityTypeBuilder<LegacyDependency> builder)
    {
        builder.ToTable("cat_legacy_dependencies", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_legacy_dependencies_source_asset_type",
                "\"SourceAssetType\" IN ('System', 'Program', 'Copybook', 'Transaction', 'Job', 'Artifact', 'Binding')");
            t.HasCheckConstraint(
                "CK_cat_legacy_dependencies_target_asset_type",
                "\"TargetAssetType\" IN ('System', 'Program', 'Copybook', 'Transaction', 'Job', 'Artifact', 'Binding')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LegacyDependencyId.From(value));

        // ── Relação ───────────────────────────────────────────────────
        builder.Property(x => x.SourceAssetType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.TargetAssetType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.DependencyType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.SourceAssetId);
        builder.HasIndex(x => x.TargetAssetId);
        builder.HasIndex(x => new { x.SourceAssetId, x.TargetAssetId, x.DependencyType }).IsUnique();

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
