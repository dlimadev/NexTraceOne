using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

internal sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    /// <summary>Configura o mapeamento da entidade Release para a tabela chg_releases.</summary>
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("chg_releases", t =>
        {
            t.HasCheckConstraint(
                "CK_chg_releases_status",
                "\"Status\" >= 0 AND \"Status\" <= 4");
            t.HasCheckConstraint(
                "CK_chg_releases_change_level",
                "\"ChangeLevel\" >= 0 AND \"ChangeLevel\" <= 4");
            t.HasCheckConstraint(
                "CK_chg_releases_change_score",
                "\"ChangeScore\" >= 0.0 AND \"ChangeScore\" <= 1.0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PipelineSource).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CommitSha).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangeLevel).HasColumnType("integer").IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired().HasDefaultValue(DeploymentStatus.Pending);
        builder.Property(x => x.ChangeScore)
            .HasColumnType("numeric(5,4)")
            .HasPrecision(5, 4)
            .IsRequired()
            .HasDefaultValue(0.0m);
        builder.Property(x => x.WorkItemReference).HasMaxLength(500);
        builder.Property(x => x.RolledBackFromReleaseId)
            .HasConversion(
                id => id != null ? (Guid?)id.Value : null,
                value => value.HasValue ? ReleaseId.From(value.Value) : null);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.EnvironmentId).HasColumnName("environment_id");

        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_chg_releases_tenant_id");
        builder.HasIndex(x => new { x.TenantId, x.EnvironmentId }).HasDatabaseName("ix_chg_releases_tenant_environment");

        // Chave natural externa — lookup sem GUID interno
        builder.Property(x => x.ExternalReleaseId).HasMaxLength(500);
        builder.Property(x => x.ExternalSystem).HasMaxLength(200);
        builder.HasIndex(x => new { x.ExternalReleaseId, x.ExternalSystem })
            .HasDatabaseName("ix_chg_releases_external_key")
            .HasFilter("\"ExternalReleaseId\" IS NOT NULL AND \"ExternalSystem\" IS NOT NULL");

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();

        // ── SLSA Level 3 evidence ─────────────────────────────────────────────
        builder.Property(x => x.SlsaProvenanceUri)
            .HasMaxLength(2000)
            .HasColumnName("slsa_provenance_uri");
        builder.Property(x => x.ArtifactDigest)
            .HasMaxLength(200)
            .HasColumnName("artifact_digest");
        builder.Property(x => x.SbomUri)
            .HasMaxLength(2000)
            .HasColumnName("sbom_uri");

        builder.HasIndex(x => x.ArtifactDigest)
            .HasDatabaseName("ix_chg_releases_artifact_digest")
            .HasFilter("\"artifact_digest\" IS NOT NULL");
    }
}
