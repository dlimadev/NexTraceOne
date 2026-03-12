using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo EngineeringGraph.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class EngineeringGraphDbContext(
    DbContextOptions<EngineeringGraphDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Ativos de API persistidos do módulo EngineeringGraph.</summary>
    public DbSet<ApiAsset> ApiAssets => Set<ApiAsset>();

    /// <summary>Ativos de serviço persistidos do módulo EngineeringGraph.</summary>
    public DbSet<ServiceAsset> ServiceAssets => Set<ServiceAsset>();

    /// <summary>Relações de consumo persistidas do módulo EngineeringGraph.</summary>
    public DbSet<ConsumerRelationship> ConsumerRelationships => Set<ConsumerRelationship>();

    /// <summary>Consumidores conhecidos persistidos do módulo EngineeringGraph.</summary>
    public DbSet<ConsumerAsset> ConsumerAssets => Set<ConsumerAsset>();

    /// <summary>Fontes de descoberta persistidas do módulo EngineeringGraph.</summary>
    public DbSet<DiscoverySource> DiscoverySources => Set<DiscoverySource>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(EngineeringGraphDbContext).Assembly;

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
