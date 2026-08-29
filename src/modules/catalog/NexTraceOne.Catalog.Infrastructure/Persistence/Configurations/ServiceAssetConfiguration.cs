using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ServiceAssetConfiguration : IEntityTypeConfiguration<ServiceAsset>
{
    public void Configure(EntityTypeBuilder<ServiceAsset> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceAssetId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Domain).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TeamName).HasMaxLength(200).IsRequired();

        // A coluna é tsvector NOT NULL mas nada a preenchia — o INSERT de ServiceAsset
        // falhava contra PostgreSQL real. Coluna gerada resolve na origem; "simple"
        // precisa casar com o PlainToTsQuery("simple", ...) usado no repositório.
        builder.HasGeneratedTsVectorColumn(
                x => x.SearchVector,
                "simple",
                x => new { x.Name, x.DisplayName, x.Description, x.TeamName, x.Domain })
            .HasIndex(x => x.SearchVector)
            .HasMethod("GIN");

        // Identidade de catálogo: nome técnico único por tenant (filtrado por soft-delete).
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.TenantId, x.LifecycleStatus });

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
