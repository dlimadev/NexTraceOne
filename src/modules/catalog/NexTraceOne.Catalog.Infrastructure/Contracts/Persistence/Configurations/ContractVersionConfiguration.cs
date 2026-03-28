using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractVersion (multi-protocolo).
/// Inclui colunas para protocolo, lifecycle state, assinatura, proveniência,
/// deprecação e relacionamentos com violations e artefatos.
/// </summary>
internal sealed class ContractVersionConfiguration : IEntityTypeConfiguration<ContractVersion>
{
    /// <summary>Configura o mapeamento da entidade ContractVersion para a tabela ctr_contract_versions.</summary>
    public void Configure(EntityTypeBuilder<ContractVersion> builder)
    {
        builder.ToTable("ctr_contract_versions", t =>
        {
            t.HasCheckConstraint(
                "CK_ctr_contract_versions_protocol",
                "\"Protocol\" IN ('OpenApi', 'Swagger', 'Wsdl', 'AsyncApi', 'Protobuf', 'GraphQL')");

            t.HasCheckConstraint(
                "CK_ctr_contract_versions_lifecycle_state",
                "\"LifecycleState\" IN ('Draft', 'InReview', 'Approved', 'Locked', 'Deprecated', 'Sunset', 'Retired')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value));
        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.SemVer).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SpecContent).HasColumnType("text").IsRequired();
        builder.Property(x => x.Format).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ImportedFrom).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.IsLocked).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.LockedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.LockedBy).HasMaxLength(500);

        // Multi-protocol e lifecycle
        builder.Property(x => x.Protocol)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ContractProtocol.OpenApi)
            .IsRequired();

        builder.Property(x => x.LifecycleState)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ContractLifecycleState.Draft)
            .IsRequired();

        // Deprecação e sunset
        builder.Property(x => x.DeprecationDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.SunsetDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeprecationNotice).HasMaxLength(2000);

        // Assinatura digital (owned entity mapeada na mesma tabela)
        builder.OwnsOne(x => x.Signature, sig =>
        {
            sig.Property(s => s.Fingerprint).HasMaxLength(128).HasColumnName("SignatureFingerprint");
            sig.Property(s => s.Algorithm).HasMaxLength(20).HasColumnName("SignatureAlgorithm");
            sig.Property(s => s.SignedBy).HasMaxLength(500).HasColumnName("SignedBy");
            sig.Property(s => s.SignedAt).HasColumnType("timestamp with time zone").HasColumnName("SignedAt");
        });

        // Proveniência (owned entity mapeada na mesma tabela)
        builder.OwnsOne(x => x.Provenance, prov =>
        {
            prov.Property(p => p.Origin).HasMaxLength(100).HasColumnName("ProvenanceOrigin");
            prov.Property(p => p.OriginalFormat).HasMaxLength(100).HasColumnName("ProvenanceOriginalFormat");
            prov.Property(p => p.ParserUsed).HasMaxLength(200).HasColumnName("ProvenanceParserUsed");
            prov.Property(p => p.StandardVersion).HasMaxLength(50).HasColumnName("ProvenanceStandardVersion");
            prov.Property(p => p.ImportedBy).HasMaxLength(500).HasColumnName("ProvenanceImportedBy");
            prov.Property(p => p.IsAiGenerated).HasColumnName("ProvenanceIsAiGenerated");
            prov.Property(p => p.AiModelVersion).HasMaxLength(200).HasColumnName("ProvenanceAiModelVersion");
        });

        // Auditoria
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.ApiAssetId, x.SemVer }).IsUnique();
        builder.HasIndex(x => x.Protocol);
        builder.HasIndex(x => x.LifecycleState);
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Relacionamentos
        builder.HasMany(x => x.Diffs)
            .WithOne()
            .HasForeignKey("ContractVersionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RuleViolations)
            .WithOne()
            .HasForeignKey(v => v.ContractVersionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Artifacts)
            .WithOne()
            .HasForeignKey(a => a.ContractVersionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
