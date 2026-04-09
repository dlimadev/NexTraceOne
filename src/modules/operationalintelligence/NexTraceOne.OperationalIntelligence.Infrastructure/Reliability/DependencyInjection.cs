using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Services;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability;

/// <summary>
/// Registra serviços de infraestrutura do subdomínio Reliability.
/// P6.1: adicionados repositórios de SLO, SLA, ErrorBudget e BurnRate.
/// P6.2: adicionado IErrorBudgetCalculator para cálculo real de error budget e burn rate.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddReliabilityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("ReliabilityDatabase", "NexTraceOne");

        services.AddDbContext<ReliabilityDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IReliabilitySnapshotRepository, ReliabilitySnapshotRepository>();
        services.AddScoped<IReliabilityRuntimeSurface, ReliabilityRuntimeSurface>();
        services.AddScoped<IReliabilityIncidentSurface, ReliabilityIncidentSurface>();

        // IUnitOfWork — resolve para ReliabilityDbContext (implementa IUnitOfWork diretamente)
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ReliabilityDbContext>());

        // P6.1 — SLO / SLA / ErrorBudget / BurnRate
        services.AddScoped<ISloDefinitionRepository, SloDefinitionRepository>();
        services.AddScoped<ISlaDefinitionRepository, SlaDefinitionRepository>();
        services.AddScoped<IErrorBudgetSnapshotRepository, ErrorBudgetSnapshotRepository>();
        services.AddScoped<IBurnRateSnapshotRepository, BurnRateSnapshotRepository>();

        // P6.2 — cálculo real de error budget e burn rate
        services.AddSingleton<IErrorBudgetCalculator, ErrorBudgetCalculator>();

        // P5.1 — Predictive Intelligence: previsões de falha e capacidade
        services.AddScoped<IServiceFailurePredictionRepository, ServiceFailurePredictionRepository>();
        services.AddScoped<ICapacityForecastRepository, CapacityForecastRepository>();

        // Ideia 12 — Predictive Incident Prevention: padrões preditivos de incidentes
        services.AddScoped<IIncidentPredictionPatternRepository, IncidentPredictionPatternRepository>();

        // P03.1 — contrato cross-module de Reliability
        services.AddScoped<IReliabilityModule, ReliabilityModuleService>();

        return services;
    }
}
