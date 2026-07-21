using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class CopybookContractMappingConfiguration : IEntityTypeConfiguration<CopybookContractMapping>
{
    public void Configure(EntityTypeBuilder<CopybookContractMapping> builder)
    {
        builder.Property(x => x.CopybookId)
            .HasConversion(id => id.Value, value => new CopybookId(value));
        builder.HasIndex(x => x.CopybookId);
    }
}
