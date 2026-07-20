using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core de <see cref="ServiceLink"/>.
/// Mapeia explicitamente a FK typed-id <c>ServiceAssetId</c> — sem esta config a
/// convenção não a mapeava e o EF caía numa coluna shadow <c>ServiceAssetId1</c>.
/// O mapeamento aponta para a coluna existente (<c>HasColumnName</c>) para não
/// mover dados, e o EF passa a usar a propriedade real como FK da navegação.
/// </summary>
internal sealed class ServiceLinkConfiguration : IEntityTypeConfiguration<ServiceLink>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceLink> builder)
    {
        builder.Property(x => x.ServiceAssetId)
            .HasConversion(id => id.Value, value => new ServiceAssetId(value))
            .HasColumnName("ServiceAssetId1");
    }
}
