using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a tabela cat_discovered_services.
/// Representa serviços descobertos automaticamente via telemetria.
/// </summary>
internal sealed class DiscoveredServiceConfiguration : IEntityTypeConfiguration<DiscoveredService>
{
    public void Configure(EntityTypeBuilder<DiscoveredService> builder)
    {
        builder.ToTable("cat_discovered_services");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DiscoveredServiceId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ServiceNamespace).HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FirstSeenAt).IsRequired();
        builder.Property(x => x.LastSeenAt).IsRequired();
        builder.Property(x => x.TraceCount).IsRequired();
        builder.Property(x => x.EndpointCount).IsRequired();
        builder.Property(x => x.DiscoveryRunId).IsRequired();
        builder.Property(x => x.IgnoreReason).HasMaxLength(500);
        builder.Property(x => x.MatchedServiceAssetId);

        builder.Property(x => x.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<DiscoveryStatus>(v))
            .HasMaxLength(50)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_cat_discovered_services_Status",
            "\"Status\" IN ('Pending', 'Matched', 'Ignored', 'Registered')"));

        // Unicidade lógica: um serviço por nome + ambiente
        builder.HasIndex(x => new { x.ServiceName, x.Environment }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Environment);
        builder.HasIndex(x => x.LastSeenAt);
    }
}
