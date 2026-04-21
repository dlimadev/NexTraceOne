using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence;

/// <summary>
/// DbContext do módulo Dependency Governance — perfis de dependências, vulnerabilidades e SBOM.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class DependencyGovernanceDbContext(
    DbContextOptions<DependencyGovernanceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IDependencyGovernanceUnitOfWork
{
    public DbSet<ServiceDependencyProfile> ServiceDependencyProfiles => Set<ServiceDependencyProfile>();
    public DbSet<PackageDependency> PackageDependencies => Set<PackageDependency>();
    public DbSet<VulnerabilityAdvisoryRecord> VulnerabilityAdvisoryRecords => Set<VulnerabilityAdvisoryRecord>();

    protected override System.Reflection.Assembly ConfigurationsAssembly => typeof(DependencyGovernanceDbContext).Assembly;
    protected override string? ConfigurationsNamespace => "NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence.Configurations";
    protected override string OutboxTableName => "dep_outbox_messages";

    public Task<int> CommitAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(cancellationToken);
}
