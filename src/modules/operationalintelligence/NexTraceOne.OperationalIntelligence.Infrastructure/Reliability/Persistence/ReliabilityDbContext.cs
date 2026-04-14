using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence;

/// <summary>
/// DbContext do subdomínio Reliability do módulo OperationalIntelligence.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// Base de dados isolada — cada sub-domínio pode ter sua própria connection string.
///
/// P6.1: expandido com entidades de SLO, SLA, ErrorBudget e BurnRate para suporte
/// a confiabilidade operacional enterprise.
/// </summary>
public sealed class ReliabilityDbContext(
    DbContextOptions<ReliabilityDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IReliabilityUnitOfWork
{
    /// <summary>Snapshots computados de confiabilidade por serviço e ambiente.</summary>
    public DbSet<ReliabilitySnapshot> ReliabilitySnapshots => Set<ReliabilitySnapshot>();

    /// <summary>Definições de SLO (Service Level Objective) por serviço e ambiente.</summary>
    public DbSet<SloDefinition> SloDefinitions => Set<SloDefinition>();

    /// <summary>Definições de SLA (Service Level Agreement) associadas a SLOs.</summary>
    public DbSet<SlaDefinition> SlaDefinitions => Set<SlaDefinition>();

    /// <summary>Snapshots do estado do error budget por SLO.</summary>
    public DbSet<ErrorBudgetSnapshot> ErrorBudgetSnapshots => Set<ErrorBudgetSnapshot>();

    /// <summary>Snapshots do burn rate do error budget por SLO e janela de tempo.</summary>
    public DbSet<BurnRateSnapshot> BurnRateSnapshots => Set<BurnRateSnapshot>();

    /// <summary>Previsões de falha de serviço computadas pelo motor de Predictive Intelligence.</summary>
    public DbSet<ServiceFailurePrediction> ServiceFailurePredictions => Set<ServiceFailurePrediction>();

    /// <summary>Previsões de capacidade de recursos por serviço e ambiente.</summary>
    public DbSet<CapacityForecast> CapacityForecasts => Set<CapacityForecast>();

    /// <summary>Padrões preditivos de incidentes identificados por análise histórica.</summary>
    public DbSet<IncidentPredictionPattern> IncidentPredictionPatterns => Set<IncidentPredictionPattern>();

    /// <summary>Recomendações de self-healing geradas a partir de causas raiz identificadas.</summary>
    public DbSet<HealingRecommendation> HealingRecommendations => Set<HealingRecommendation>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ReliabilityDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "ops_rel_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
