using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractPublicationEntry.
/// Armazena as entradas do Publication Center que controlam a exposição de contratos no Developer Portal.
/// Tabela: cat_portal_contract_publications.
/// </summary>
internal sealed class ContractPublicationEntryConfiguration : IEntityTypeConfiguration<ContractPublicationEntry>
{
    /// <summary>Configura a entidade ContractPublicationEntry.</summary>
    public void Configure(EntityTypeBuilder<ContractPublicationEntry> builder)
    {
        builder.ToTable("cat_portal_contract_publications", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_portal_contract_publications_status",
                "\"Status\" IN ('PendingPublication', 'Published', 'Withdrawn', 'Deprecated')");
            t.HasCheckConstraint(
                "CK_cat_portal_contract_publications_visibility",
                "\"Visibility\" IN ('Internal', 'External', 'RestrictedToTeams')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractPublicationEntryId.From(value));

        builder.Property(x => x.ContractVersionId).IsRequired();
        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ContractTitle).HasMaxLength(300).IsRequired();
        builder.Property(x => x.SemVer).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.PublishedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PublishedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReleaseNotes).HasMaxLength(2000);
        builder.Property(x => x.WithdrawnBy).HasMaxLength(200);
        builder.Property(x => x.WithdrawnAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.WithdrawalReason).HasMaxLength(500);

        // Auditoria
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        // Um único entry por ContractVersion (1:0..1)
        builder.HasIndex(x => x.ContractVersionId).IsUnique();
        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");
    }
}
