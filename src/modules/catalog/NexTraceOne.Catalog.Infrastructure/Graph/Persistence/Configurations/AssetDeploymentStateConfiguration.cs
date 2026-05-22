using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para AssetDeploymentState.
/// Uma linha por (ServiceAssetId, Environment) — semântica de upsert.
/// </summary>
internal sealed class AssetDeploymentStateConfiguration : IEntityTypeConfiguration<AssetDeploymentState>
{
    public void Configure(EntityTypeBuilder<AssetDeploymentState> builder)
    {
        builder.ToTable("cat_asset_deployment_states");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AssetDeploymentStateId.From(value));

        builder.Property(x => x.ServiceAssetId)
            .HasConversion(id => id.Value, value => ServiceAssetId.From(value))
            .IsRequired();

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.Environment)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ImageTag)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ReleaseName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.RuntimeStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LastHeartbeatAt).IsRequired();
        builder.Property(x => x.DeployedAt).IsRequired();

        // Uma linha por serviço+ambiente
        builder.HasIndex(x => new { x.ServiceAssetId, x.Environment }).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.DeployedAt).IsDescending();
    }
}
