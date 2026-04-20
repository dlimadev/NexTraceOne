using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

internal sealed class ServiceAssetConfiguration : IEntityTypeConfiguration<ServiceAsset>
{
    public void Configure(EntityTypeBuilder<ServiceAsset> builder)
    {
        builder.ToTable("cat_service_assets", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_service_assets_service_type",
                "\"ServiceType\" IN ('RestApi', 'SoapService', 'KafkaProducer', 'KafkaConsumer', 'BackgroundService', 'ScheduledProcess', 'IntegrationComponent', 'SharedPlatformService', 'GraphqlApi', 'GrpcService', 'LegacySystem', 'Gateway', 'ThirdParty', 'CobolProgram', 'CicsTransaction', 'ImsTransaction', 'BatchJob', 'MainframeSystem', 'MqQueueManager', 'ZosConnectApi', 'Framework')");
            t.HasCheckConstraint(
                "CK_cat_service_assets_criticality",
                "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
            t.HasCheckConstraint(
                "CK_cat_service_assets_lifecycle_status",
                "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
            t.HasCheckConstraint(
                "CK_cat_service_assets_exposure_type",
                "\"ExposureType\" IN ('Internal', 'Partner', 'Public')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceAssetId.From(value));

        // ── Identidade ────────────────────────────────────────────────
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).HasDefaultValue(string.Empty);
        builder.Property(x => x.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);
        builder.Property(x => x.ServiceType).HasConversion<string>().HasMaxLength(50).HasDefaultValue(ServiceType.RestApi);
        builder.Property(x => x.Domain).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SystemArea).HasMaxLength(200).HasDefaultValue(string.Empty);

        // ── Ownership ─────────────────────────────────────────────────
        builder.Property(x => x.TeamName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TechnicalOwner).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.BusinessOwner).HasMaxLength(200).HasDefaultValue(string.Empty);

        // ── Classificação ─────────────────────────────────────────────
        builder.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(50).HasDefaultValue(Criticality.Medium);
        builder.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(50).HasDefaultValue(LifecycleStatus.Active);
        builder.Property(x => x.ExposureType).HasConversion<string>().HasMaxLength(50).HasDefaultValue(ExposureType.Internal);

        // ── Governança ────────────────────────────────────────────────
        builder.Property(x => x.DocumentationUrl).HasMaxLength(1000).HasDefaultValue(string.Empty);
        builder.Property(x => x.RepositoryUrl).HasMaxLength(1000).HasDefaultValue(string.Empty);

        // ── Metadados estendidos ──────────────────────────────────────
        builder.Property(x => x.SubDomain).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.Capability).HasMaxLength(300).IsRequired(false);
        builder.Property(x => x.GitRepository).HasMaxLength(1000).HasDefaultValue(string.Empty);
        builder.Property(x => x.CiPipelineUrl).HasMaxLength(1000).HasDefaultValue(string.Empty);
        builder.Property(x => x.InfrastructureProvider).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.HostingPlatform).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.RuntimeLanguage).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(x => x.RuntimeVersion).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(x => x.SloTarget).HasMaxLength(50).HasDefaultValue(string.Empty);
        builder.Property(x => x.DataClassification).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(x => x.RegulatoryScope).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.ChangeFrequency).HasMaxLength(50).HasDefaultValue(string.Empty);
        builder.Property(x => x.ProductOwner).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.ContactChannel).HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(x => x.OnCallRotationId).HasMaxLength(200).HasDefaultValue(string.Empty);

        builder.Property(x => x.Tier)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue("Standard");

        builder.Property(x => x.LastOwnershipReviewAt)
            .IsRequired(false);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.TeamName);
        builder.HasIndex(x => x.Domain);
        builder.HasIndex(x => x.SubDomain);
        builder.HasIndex(x => x.ServiceType);
        builder.HasIndex(x => x.Criticality);
        builder.HasIndex(x => x.LifecycleStatus);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
