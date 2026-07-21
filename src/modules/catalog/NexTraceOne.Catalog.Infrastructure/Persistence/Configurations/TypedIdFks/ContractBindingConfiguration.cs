using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class ContractBindingConfiguration : IEntityTypeConfiguration<ContractBinding>
{
    public void Configure(EntityTypeBuilder<ContractBinding> builder)
    {
        builder.Property(x => x.ServiceInterfaceId)
            .HasConversion(id => id.Value, value => new ServiceInterfaceId(value));
        builder.HasIndex(x => x.ServiceInterfaceId);
    }
}
