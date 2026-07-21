using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class AssetDeploymentStateConfiguration : IEntityTypeConfiguration<AssetDeploymentState>
{
    public void Configure(EntityTypeBuilder<AssetDeploymentState> builder)
    {
        builder.Property(x => x.ServiceAssetId)
            .HasConversion(id => id.Value, value => new ServiceAssetId(value));
        builder.HasIndex(x => x.ServiceAssetId);
    }
}
