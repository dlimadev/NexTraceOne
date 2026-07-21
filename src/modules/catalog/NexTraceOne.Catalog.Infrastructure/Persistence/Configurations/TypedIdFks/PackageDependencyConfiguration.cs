using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.DependencyGovernance;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeia a FK typed-id <c>ProfileId</c> à coluna já existente <c>ServiceDependencyProfileId</c>
/// (criada como shadow pela navegação-coleção do principal). A relação é reconfigurada em
/// <see cref="ServiceDependencyProfileConfiguration"/> para usar esta propriedade como FK,
/// evitando uma segunda coluna shadow. Ver reference-typed-id-fk-mapping-gap.
/// </summary>
internal sealed class PackageDependencyConfiguration : IEntityTypeConfiguration<PackageDependency>
{
    public void Configure(EntityTypeBuilder<PackageDependency> builder)
    {
        builder.Property(x => x.ProfileId)
            .HasConversion(id => id.Value, value => new ServiceDependencyProfileId(value))
            .HasColumnName("ServiceDependencyProfileId");
    }
}
