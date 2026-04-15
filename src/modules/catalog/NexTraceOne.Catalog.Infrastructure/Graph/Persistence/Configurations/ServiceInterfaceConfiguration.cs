using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ServiceInterface.
/// Interfaces representam os pontos de contrato concretos de cada serviço,
/// com tipo de protocolo, estado, autenticação e vinculação a contratos.
/// </summary>
internal sealed class ServiceInterfaceConfiguration : IEntityTypeConfiguration<ServiceInterface>
{
    public void Configure(EntityTypeBuilder<ServiceInterface> builder)
    {
        builder.ToTable("cat_service_interfaces");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceInterfaceId.From(value));

        // ── Referência ao serviço ─────────────────────────────────────
        builder.Property(x => x.ServiceAssetId).IsRequired();

        // ── Identidade ────────────────────────────────────────────────
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);

        // ── Tipo e estado ─────────────────────────────────────────────
        builder.Property(x => x.InterfaceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).HasDefaultValue(InterfaceStatus.Active);
        builder.Property(x => x.ExposureScope).HasConversion<string>().HasMaxLength(50).HasDefaultValue(ExposureType.Internal);

        // ── Detalhes por protocolo ────────────────────────────────────
        builder.Property(x => x.BasePath).HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(x => x.TopicName).HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(x => x.WsdlNamespace).HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(x => x.GrpcServiceName).HasMaxLength(300).HasDefaultValue(string.Empty);
        builder.Property(x => x.ScheduleCron).HasMaxLength(200).HasDefaultValue(string.Empty);

        // ── Contexto operacional ──────────────────────────────────────
        builder.Property(x => x.EnvironmentId).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.SloTarget).HasMaxLength(50).HasDefaultValue(string.Empty);
        builder.Property(x => x.RequiresContract).HasDefaultValue(false);

        // ── Segurança ─────────────────────────────────────────────────
        builder.Property(x => x.AuthScheme).HasConversion<string>().HasMaxLength(50).HasDefaultValue(InterfaceAuthScheme.None);
        builder.Property(x => x.RateLimitPolicy).HasMaxLength(500).HasDefaultValue(string.Empty);

        // ── Documentação ──────────────────────────────────────────────
        builder.Property(x => x.DocumentationUrl).HasMaxLength(1000).HasDefaultValue(string.Empty);

        // ── Deprecação ────────────────────────────────────────────────
        builder.Property(x => x.DeprecationDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.SunsetDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeprecationNotice).HasMaxLength(2000);

        // ── Auditoria ─────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // ── FK para ServiceAsset ──────────────────────────────────────
        builder.HasOne<ServiceAsset>()
            .WithMany()
            .HasForeignKey(x => x.ServiceAssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.ServiceAssetId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.InterfaceType);
        builder.HasIndex(x => new { x.ServiceAssetId, x.Status, x.InterfaceType });
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");

        // ── Concorrência otimista ─────────────────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
