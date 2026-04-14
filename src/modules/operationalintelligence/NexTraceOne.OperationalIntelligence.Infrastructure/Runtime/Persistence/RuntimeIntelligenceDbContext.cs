using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;

/// <summary>
/// DbContext do módulo RuntimeIntelligence.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// Base de dados isolada por serviço — cada módulo possui sua própria connection string.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class RuntimeIntelligenceDbContext(
    DbContextOptions<RuntimeIntelligenceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IRuntimeIntelligenceUnitOfWork
{
    /// <summary>Snapshots de saúde e performance de serviços em runtime.</summary>
    public DbSet<RuntimeSnapshot> RuntimeSnapshots => Set<RuntimeSnapshot>();

    /// <summary>Baselines de métricas de runtime para comparação de drift.</summary>
    public DbSet<RuntimeBaseline> RuntimeBaselines => Set<RuntimeBaseline>();

    /// <summary>Findings de drift detectados entre baselines e snapshots atuais.</summary>
    public DbSet<DriftFinding> DriftFindings => Set<DriftFinding>();

    /// <summary>Perfis de maturidade de observabilidade por serviço.</summary>
    public DbSet<ObservabilityProfile> ObservabilityProfiles => Set<ObservabilityProfile>();

    /// <summary>Gráficos customizados criados pelos utilizadores.</summary>
    public DbSet<CustomChart> CustomCharts => Set<CustomChart>();

    /// <summary>Experimentos de chaos engineering planeados e executados.</summary>
    public DbSet<ChaosExperiment> ChaosExperiments => Set<ChaosExperiment>();

    /// <summary>Narrativas de anomalia geradas por IA a partir de drift findings.</summary>
    public DbSet<AnomalyNarrative> AnomalyNarratives => Set<AnomalyNarrative>();

    /// <summary>Relatórios de drift entre ambientes.</summary>
    public DbSet<EnvironmentDriftReport> EnvironmentDriftReports => Set<EnvironmentDriftReport>();

    /// <summary>Playbooks operacionais estruturados e versionáveis.</summary>
    public DbSet<OperationalPlaybook> OperationalPlaybooks => Set<OperationalPlaybook>();

    /// <summary>Registos de execução de playbooks operacionais.</summary>
    public DbSet<PlaybookExecution> PlaybookExecutions => Set<PlaybookExecution>();

    /// <summary>Relatórios de resiliência gerados após experimentos de chaos.</summary>
    public DbSet<ResilienceReport> ResilienceReports => Set<ResilienceReport>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(RuntimeIntelligenceDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "ops_rt_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
