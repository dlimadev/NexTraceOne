using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class Db2ArtifactConfiguration : IEntityTypeConfiguration<Db2Artifact>
{
    public void Configure(EntityTypeBuilder<Db2Artifact> builder)
    {
        builder.ToTable("cat_db2_artifacts", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_db2_artifacts_artifact_type",
                "\"ArtifactType\" IN ('Table', 'View', 'StoredProcedure', 'Index', 'Tablespace', 'Package')");
            t.HasCheckConstraint(
                "CK_cat_db2_artifacts_criticality",
                "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
            t.HasCheckConstraint(
                "CK_cat_db2_artifacts_lifecycle_status",
                "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => Db2ArtifactId.From(value));

        // ── Identidade ────────────────────────────────────────────────
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).HasDefaultValue(string.Empty);
        builder.Property(x => x.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);

        // ── FK para MainframeSystem ───────────────────────────────────
        builder.Property(x => x.SystemId)
            .HasConversion(id => id.Value, value => MainframeSystemId.From(value));
        builder.HasOne<MainframeSystem>()
            .WithMany()
            .HasForeignKey(x => x.SystemId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── DB2 ───────────────────────────────────────────────────────
        builder.Property(x => x.ArtifactType).HasConversion<string>().HasMaxLength(50).HasDefaultValue(Db2ArtifactType.Table);
        builder.Property(x => x.SchemaName).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.TablespaceName).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.DatabaseName).HasMaxLength(200).HasDefaultValue(string.Empty);

        // ── Classificação ─────────────────────────────────────────────
        builder.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(50).HasDefaultValue(Criticality.Medium);
        builder.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(50).HasDefaultValue(LifecycleStatus.Active);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => new { x.Name, x.SystemId }).IsUnique();
        builder.HasIndex(x => x.SystemId);
        builder.HasIndex(x => x.ArtifactType);
        builder.HasIndex(x => x.Criticality);
        builder.HasIndex(x => x.LifecycleStatus);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
