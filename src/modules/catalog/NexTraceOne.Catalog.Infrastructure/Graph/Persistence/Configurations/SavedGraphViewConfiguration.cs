using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para SavedGraphView.
/// Define tabela, chave primária, índices e constraints para visões salvas do grafo.
/// </summary>
internal sealed class SavedGraphViewConfiguration : IEntityTypeConfiguration<SavedGraphView>
{
    public void Configure(EntityTypeBuilder<SavedGraphView> builder)
    {
        builder.ToTable("saved_graph_views");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SavedGraphViewId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.OwnerId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsShared).IsRequired();
        builder.Property(x => x.FiltersJson).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => new { x.OwnerId, x.IsShared });
    }
}
