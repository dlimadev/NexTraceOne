using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability;

/// <summary>
/// Registra serviços de infraestrutura do subdomínio Reliability.
/// P6.1: adicionados repositórios de SLO, SLA, ErrorBudget e BurnRate.
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

        // P6.1 — SLO / SLA / ErrorBudget / BurnRate
        services.AddScoped<ISloDefinitionRepository, SloDefinitionRepository>();
        services.AddScoped<ISlaDefinitionRepository, SlaDefinitionRepository>();
        services.AddScoped<IErrorBudgetSnapshotRepository, ErrorBudgetSnapshotRepository>();
        services.AddScoped<IBurnRateSnapshotRepository, BurnRateSnapshotRepository>();

        return services;
    }
}
