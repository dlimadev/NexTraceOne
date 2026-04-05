using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

internal sealed class FrameworkAssetDetailConfiguration : IEntityTypeConfiguration<FrameworkAssetDetail>
{
    public void Configure(EntityTypeBuilder<FrameworkAssetDetail> builder)
    {
        builder.ToTable("cat_framework_asset_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => FrameworkAssetDetailId.From(value));

        // ── Referência ao serviço ─────────────────────────────────────
        builder.Property(x => x.ServiceAssetId)
            .HasConversion(id => id.Value, value => ServiceAssetId.From(value))
            .IsRequired();

        builder.HasIndex(x => x.ServiceAssetId).IsUnique();

        // ── Identidade do Framework ───────────────────────────────────
        builder.Property(x => x.PackageName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PackageManager).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ArtifactRegistryUrl).HasMaxLength(1000).HasDefaultValue(string.Empty);

        // ── Versão e Compatibilidade ──────────────────────────────────
        builder.Property(x => x.LatestVersion).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(x => x.MinSupportedVersion).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(x => x.TargetPlatform).HasMaxLength(200).HasDefaultValue(string.Empty);

        // ── Metadata ──────────────────────────────────────────────────
        builder.Property(x => x.LicenseType).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(x => x.BuildPipelineUrl).HasMaxLength(1000).HasDefaultValue(string.Empty);
        builder.Property(x => x.ChangelogUrl).HasMaxLength(1000).HasDefaultValue(string.Empty);

        // ── Relações ──────────────────────────────────────────────────
        builder.Property(x => x.KnownConsumerCount).HasDefaultValue(0);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.PackageName);
        builder.HasIndex(x => x.Language);
    }
}
