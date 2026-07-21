using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class CanonicalEntityVersionConfiguration : IEntityTypeConfiguration<CanonicalEntityVersion>
{
    public void Configure(EntityTypeBuilder<CanonicalEntityVersion> builder)
    {
        builder.Property(x => x.CanonicalEntityId)
            .HasConversion(id => id.Value, value => new CanonicalEntityId(value));
        builder.HasIndex(x => x.CanonicalEntityId);
    }
}
