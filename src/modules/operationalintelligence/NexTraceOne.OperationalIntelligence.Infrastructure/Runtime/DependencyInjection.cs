using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime;

/// <summary>
/// Registra serviços de infraestrutura do módulo RuntimeIntelligence.
/// Inclui: DbContext com connection string isolada, repositórios e UnitOfWork.
/// Cada módulo possui sua própria base de dados — sem compartilhamento.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo RuntimeIntelligence ao container DI.</summary>
    public static IServiceCollection AddRuntimeIntelligenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("RuntimeIntelligenceDatabase", "NexTraceOne");

        services.AddDbContext<RuntimeIntelligenceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RuntimeIntelligenceDbContext>());
        services.AddScoped<IRuntimeSnapshotRepository, RuntimeSnapshotRepository>();
        services.AddScoped<IRuntimeBaselineRepository, RuntimeBaselineRepository>();
        services.AddScoped<IDriftFindingRepository, DriftFindingRepository>();
        services.AddScoped<IObservabilityProfileRepository, ObservabilityProfileRepository>();

        // ── Incidents (Incident Correlation & Mitigation) infrastructure ──
        services.AddIncidentsInfrastructure(configuration);

        // ── Automation (Workflow Persistence) infrastructure ──
        services.AddAutomationInfrastructure(configuration);

        return services;
    }
}
