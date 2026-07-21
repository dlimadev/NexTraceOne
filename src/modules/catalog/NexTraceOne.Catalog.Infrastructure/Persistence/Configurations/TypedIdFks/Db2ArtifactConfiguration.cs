using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class Db2ArtifactConfiguration : IEntityTypeConfiguration<Db2Artifact>
{
    public void Configure(EntityTypeBuilder<Db2Artifact> builder)
    {
        builder.Property(x => x.SystemId)
            .HasConversion(id => id.Value, value => new MainframeSystemId(value));
        builder.HasIndex(x => x.SystemId);
    }
}
