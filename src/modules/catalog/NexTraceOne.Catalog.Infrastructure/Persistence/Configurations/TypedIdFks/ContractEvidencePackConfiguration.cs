using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class ContractEvidencePackConfiguration : IEntityTypeConfiguration<ContractEvidencePack>
{
    public void Configure(EntityTypeBuilder<ContractEvidencePack> builder)
    {
        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => new ContractVersionId(value));
        builder.HasIndex(x => x.ContractVersionId);
    }
}
