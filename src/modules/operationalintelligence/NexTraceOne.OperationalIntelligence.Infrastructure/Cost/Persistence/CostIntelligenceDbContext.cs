using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;

/// <summary>
/// DbContext do módulo CostIntelligence.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// Base de dados isolada por serviço — cada módulo possui sua própria connection string.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class CostIntelligenceDbContext(
    DbContextOptions<CostIntelligenceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Snapshots de custo de infraestrutura capturados periodicamente.</summary>
    public DbSet<CostSnapshot> CostSnapshots => Set<CostSnapshot>();

    /// <summary>Atribuições de custo a serviços/APIs específicos por período.</summary>
    public DbSet<CostAttribution> CostAttributions => Set<CostAttribution>();

    /// <summary>Análises de tendência de custo ao longo do tempo.</summary>
    public DbSet<CostTrend> CostTrends => Set<CostTrend>();

    /// <summary>Perfis de custo de serviços com orçamento e alertas.</summary>
    public DbSet<ServiceCostProfile> ServiceCostProfiles => Set<ServiceCostProfile>();

    /// <summary>Batches de importação de registos de custo.</summary>
    public DbSet<CostImportBatch> CostImportBatches => Set<CostImportBatch>();

    /// <summary>Registos individuais de custo importados com atribuição a serviço/equipa/domínio.</summary>
    public DbSet<CostRecord> CostRecords => Set<CostRecord>();

    /// <summary>Previsões de orçamento calculadas para serviços por período.</summary>
    public DbSet<BudgetForecast> BudgetForecasts => Set<BudgetForecast>();

    /// <summary>Recomendações de eficiência de custo geradas por análise comparativa.</summary>
    public DbSet<EfficiencyRecommendation> EfficiencyRecommendations => Set<EfficiencyRecommendation>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(CostIntelligenceDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "ops_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
