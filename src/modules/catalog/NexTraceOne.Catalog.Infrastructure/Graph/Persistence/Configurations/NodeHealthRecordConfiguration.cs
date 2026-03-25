using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para NodeHealthRecord.
/// Define tabela, chave primária, índices e conversão de enums para overlays explicáveis.
/// </summary>
internal sealed class NodeHealthRecordConfiguration : IEntityTypeConfiguration<NodeHealthRecord>
{
    public void Configure(EntityTypeBuilder<NodeHealthRecord> builder)
    {
        builder.ToTable("cat_node_health_records");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => NodeHealthRecordId.From(value));

        builder.Property(x => x.NodeId).IsRequired();
        builder.Property(x => x.NodeType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.OverlayMode).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Score).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.FactorsJson).IsRequired();
        builder.Property(x => x.CalculatedAt).IsRequired();
        builder.Property(x => x.SourceSystem).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => new { x.NodeId, x.OverlayMode });
        builder.HasIndex(x => x.CalculatedAt).IsDescending();
    }
}
