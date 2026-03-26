using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Governance.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
///
/// P2.1: IntegrationConnectors extraído para IntegrationsDbContext.
/// P2.2: IngestionSources e IngestionExecutions extraídos para IntegrationsDbContext.
/// OI-03 (pendente): AnalyticsEvents a extrair para módulo próprio.
/// Após OI-03, apenas os 8 DbSets de Governance permanecerão.
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

    // NOTE: IntegrationConnectors extracted to IntegrationsDbContext in P2.1.
    // NOTE: IngestionSources extracted to IntegrationsDbContext in P2.2.
    // NOTE: IngestionExecutions extracted to IntegrationsDbContext in P2.2.

    /// <summary>Eventos de Product Analytics (a ser extraído para módulo próprio em OI-03).</summary>
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(GovernanceDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "gov_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
