using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Contracts.Domain.Entities;

namespace NexTraceOne.Contracts.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Contracts.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ContractsDbContext(
    DbContextOptions<ContractsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Versões de contrato OpenAPI persistidas no módulo Contracts.</summary>
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();

    /// <summary>Diffs semânticos entre versões de contrato persistidos no módulo Contracts.</summary>
    public DbSet<ContractDiff> ContractDiffs => Set<ContractDiff>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ContractsDbContext).Assembly;

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
