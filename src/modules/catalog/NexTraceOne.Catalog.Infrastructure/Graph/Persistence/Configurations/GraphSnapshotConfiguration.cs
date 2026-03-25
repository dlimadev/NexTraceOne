using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para GraphSnapshot.
/// Define tabela, chave primária, índices e constraints para snapshots temporais.
/// </summary>
internal sealed class GraphSnapshotConfiguration : IEntityTypeConfiguration<GraphSnapshot>
{
    public void Configure(EntityTypeBuilder<GraphSnapshot> builder)
    {
        builder.ToTable("cat_graph_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => GraphSnapshotId.From(value));

        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CapturedAt).IsRequired();
        builder.Property(x => x.NodesJson).IsRequired();
        builder.Property(x => x.EdgesJson).IsRequired();
        builder.Property(x => x.NodeCount).IsRequired();
        builder.Property(x => x.EdgeCount).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.CapturedAt).IsDescending();
    }
}
