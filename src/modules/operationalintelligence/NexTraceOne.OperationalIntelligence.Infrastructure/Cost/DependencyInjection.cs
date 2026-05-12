using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Services;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost;

/// <summary>
/// Registra serviços de infraestrutura do módulo CostIntelligence.
/// Inclui: DbContext com connection string isolada, repositórios e UnitOfWork.
/// Cada módulo possui sua própria base de dados — sem compartilhamento.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo CostIntelligence ao container DI.</summary>
    public static IServiceCollection AddCostIntelligenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("CostIntelligenceDatabase", "NexTraceOne");

        services.AddDbContext<CostIntelligenceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CostIntelligenceDbContext>());
        services.AddScoped<ICostIntelligenceUnitOfWork>(sp => sp.GetRequiredService<CostIntelligenceDbContext>());
        services.AddScoped<ICostSnapshotRepository, CostSnapshotRepository>();
        services.AddScoped<ICostAttributionRepository, CostAttributionRepository>();
        services.AddScoped<IServiceCostProfileRepository, ServiceCostProfileRepository>();
        services.AddScoped<ICostImportBatchRepository, CostImportBatchRepository>();
        services.AddScoped<ICostRecordRepository, CostRecordRepository>();
        // P6.3 — repositório de tendências de custo (necessário para ComputeCostTrend persistir)
        services.AddScoped<ICostTrendRepository, CostTrendRepository>();
        services.AddScoped<IBudgetForecastRepository, BudgetForecastRepository>();
        services.AddScoped<IEfficiencyRecommendationRepository, EfficiencyRecommendationRepository>();
        services.AddScoped<IWasteSignalRepository, WasteSignalRepository>();
        services.AddScoped<ICarbonScoreRepository, CarbonScoreRepository>();
        services.AddScoped<ICostIntelligenceModule, CostIntelligenceModuleService>();

        // ── FinOps Report null readers ────────────────────────────────────
        services.AddScoped<
            NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsWasteAnalysisReport.GetFinOpsWasteAnalysisReport.IFinOpsWasteReader,
            NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsWasteAnalysisReport.GetFinOpsWasteAnalysisReport.NullFinOpsWasteReader>();
        services.AddScoped<
            NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetEnvironmentCostComparisonReport.GetEnvironmentCostComparisonReport.IEnvironmentCostComparisonReader,
            NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetEnvironmentCostComparisonReport.GetEnvironmentCostComparisonReport.NullEnvironmentCostComparisonReader>();
        services.AddScoped<
            NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetCostPerReleaseReport.GetCostPerReleaseReport.ICostPerReleaseReader,
            NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetCostPerReleaseReport.GetCostPerReleaseReport.NullCostPerReleaseReader>();

        return services;
    }
}
