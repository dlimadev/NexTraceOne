using Microsoft.EntityFrameworkCore;

using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence;

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

    /// <summary>Políticas de compliance.</summary>
    public DbSet<CompliancePolicy> CompliancePolicies => Set<CompliancePolicy>();

    /// <summary>Campanhas de auditoria.</summary>
    public DbSet<AuditCampaign> AuditCampaigns => Set<AuditCampaign>();

    /// <summary>Resultados de compliance.</summary>
    public DbSet<ComplianceResult> ComplianceResults => Set<ComplianceResult>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(AuditDbContext).Assembly;

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
