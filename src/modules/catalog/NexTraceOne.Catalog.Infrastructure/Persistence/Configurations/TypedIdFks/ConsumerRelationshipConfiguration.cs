using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class ConsumerRelationshipConfiguration : IEntityTypeConfiguration<ConsumerRelationship>
{
    public void Configure(EntityTypeBuilder<ConsumerRelationship> builder)
    {
        builder.Property(x => x.ConsumerAssetId)
            .HasConversion(id => id.Value, value => new ConsumerAssetId(value));
        builder.HasIndex(x => x.ConsumerAssetId);
    }
}
