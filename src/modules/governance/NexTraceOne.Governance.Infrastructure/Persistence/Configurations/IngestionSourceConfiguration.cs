using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade IngestionSource.
/// Define mapeamento de tabela, typed ID, enums, relacionamentos e índices.
/// NOTA P2.1: A navegação para IntegrationConnector foi removida desta configuração porque
/// IntegrationConnector passou a pertencer ao IntegrationsDbContext.
/// ConnectorId mantém-se como chave estrangeira a nível de base de dados,
/// mas sem navegação EF Core entre DbContexts.
/// </summary>
internal sealed class IngestionSourceConfiguration : IEntityTypeConfiguration<IngestionSource>
{
    public void Configure(EntityTypeBuilder<IngestionSource> builder)
    {
        builder.ToTable("int_ingestion_sources", t =>
        {
            t.HasCheckConstraint("CK_int_ingestion_sources_status",
                "\"Status\" IN ('Pending','Active','Paused','Disabled','Error')");
            t.HasCheckConstraint("CK_int_ingestion_sources_freshness_status",
                "\"FreshnessStatus\" IN ('Unknown','Fresh','Stale','Outdated','Expired')");
            t.HasCheckConstraint("CK_int_ingestion_sources_trust_level",
                "\"TrustLevel\" IN ('Unverified','Basic','Verified','Trusted','Official')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new IngestionSourceId(value));

        builder.Property(x => x.ConnectorId)
            .HasConversion(id => id.Value, value => new IntegrationConnectorId(value))
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DataDomain)
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("");

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Endpoint)
            .HasMaxLength(500);

        builder.Property(x => x.TrustLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FreshnessStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LastDataReceivedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastProcessedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DataItemsProcessed)
            .IsRequired();

        builder.Property(x => x.ExpectedIntervalMinutes);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // NOTE P2.1: FK navigation to IntegrationConnector removed because IntegrationConnector
        // moved to IntegrationsDbContext. The FK column ConnectorId is preserved as a cross-context
        // reference. EF navigation will be restored when IngestionSource is extracted in P2.2.
        builder.HasIndex(x => x.ConnectorId);
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.SourceType);
        builder.HasIndex(x => x.DataDomain);
        builder.HasIndex(x => x.TrustLevel);
        builder.HasIndex(x => x.FreshnessStatus);
        builder.HasIndex(x => x.Status);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
