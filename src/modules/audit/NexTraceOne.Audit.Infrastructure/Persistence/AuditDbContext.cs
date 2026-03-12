using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Audit.Domain.Entities;

namespace NexTraceOne.Audit.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Audit.
/// REGRA: Outros módulos NUNCA referenciam este DbContext.
/// </summary>
public sealed class AuditDbContext(
    DbContextOptions<AuditDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Eventos de auditoria.</summary>
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    /// <summary>Links da cadeia de hash.</summary>
    public DbSet<AuditChainLink> AuditChainLinks => Set<AuditChainLink>();

    /// <summary>Políticas de retenção.</summary>
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(AuditDbContext).Assembly;

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
