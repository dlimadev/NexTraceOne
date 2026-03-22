using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence;

/// <summary>
/// DbContext do subdomínio Reliability do módulo OperationalIntelligence.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// Base de dados isolada — cada sub-domínio pode ter sua própria connection string.
/// </summary>
public sealed class ReliabilityDbContext(
    DbContextOptions<ReliabilityDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Snapshots computados de confiabilidade por serviço e ambiente.</summary>
    public DbSet<ReliabilitySnapshot> ReliabilitySnapshots => Set<ReliabilitySnapshot>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ReliabilityDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
