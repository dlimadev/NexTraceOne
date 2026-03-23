using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Governance.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class GovernanceDbContext(
    DbContextOptions<GovernanceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Equipas da organização.</summary>
    public DbSet<Team> Teams => Set<Team>();

    /// <summary>Domínios de governança (áreas de negócio/técnicas).</summary>
    public DbSet<GovernanceDomain> Domains => Set<GovernanceDomain>();

    /// <summary>Pacotes de regras de governança.</summary>
    public DbSet<GovernancePack> Packs => Set<GovernancePack>();

    /// <summary>Versões de pacotes de governança.</summary>
    public DbSet<GovernancePackVersion> PackVersions => Set<GovernancePackVersion>();

    /// <summary>Waivers (exceções) de regras de governança.</summary>
    public DbSet<GovernanceWaiver> Waivers => Set<GovernanceWaiver>();

    /// <summary>Delegações de administração.</summary>
    public DbSet<DelegatedAdministration> DelegatedAdministrations => Set<DelegatedAdministration>();

    /// <summary>Associações equipa-domínio.</summary>
    public DbSet<TeamDomainLink> TeamDomainLinks => Set<TeamDomainLink>();

    /// <summary>Registos de rollout de pacotes de governança.</summary>
    public DbSet<GovernanceRolloutRecord> RolloutRecords => Set<GovernanceRolloutRecord>();

    /// <summary>Connectors de integração com sistemas externos.</summary>
    public DbSet<IntegrationConnector> IntegrationConnectors => Set<IntegrationConnector>();

    /// <summary>Fontes de ingestão de dados.</summary>
    public DbSet<IngestionSource> IngestionSources => Set<IngestionSource>();

    /// <summary>Execuções de ingestão de dados.</summary>
    public DbSet<IngestionExecution> IngestionExecutions => Set<IngestionExecution>();

    /// <summary>Eventos de Product Analytics.</summary>
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(GovernanceDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "gov_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
