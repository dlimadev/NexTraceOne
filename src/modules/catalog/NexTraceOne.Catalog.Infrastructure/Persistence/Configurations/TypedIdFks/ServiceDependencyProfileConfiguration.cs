using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reconfigura a navegação-coleção <c>Dependencies</c> para usar a propriedade typed-id
/// <c>PackageDependency.ProfileId</c> como chave estrangeira, reutilizando a coluna
/// <c>ServiceDependencyProfileId</c> já existente em vez de gerar uma coluna shadow duplicada.
/// Ver reference-typed-id-fk-mapping-gap.
/// </summary>
internal sealed class ServiceDependencyProfileConfiguration : IEntityTypeConfiguration<ServiceDependencyProfile>
{
    public void Configure(EntityTypeBuilder<ServiceDependencyProfile> builder)
    {
        builder.HasMany(p => p.Dependencies)
            .WithOne()
            .HasForeignKey(d => d.ProfileId)
            .IsRequired();
    }
}
