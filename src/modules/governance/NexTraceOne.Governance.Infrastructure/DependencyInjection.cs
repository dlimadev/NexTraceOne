using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.Governance.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Governance.
/// Inclui: DbContext, Repositórios, UnitOfWork.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Governance.</summary>
    public static IServiceCollection AddGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("GovernanceDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

        services.AddDbContext<GovernanceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        // UnitOfWork
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GovernanceDbContext>());

        // Repositories
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IGovernanceDomainRepository, GovernanceDomainRepository>();
        services.AddScoped<IGovernancePackRepository, GovernancePackRepository>();
        services.AddScoped<IGovernancePackVersionRepository, GovernancePackVersionRepository>();
        services.AddScoped<IGovernanceWaiverRepository, GovernanceWaiverRepository>();
        services.AddScoped<IDelegatedAdministrationRepository, DelegatedAdministrationRepository>();
        services.AddScoped<ITeamDomainLinkRepository, TeamDomainLinkRepository>();
        services.AddScoped<IGovernanceRolloutRecordRepository, GovernanceRolloutRecordRepository>();
        services.AddScoped<IIntegrationConnectorRepository, IntegrationConnectorRepository>();
        services.AddScoped<IIngestionSourceRepository, IngestionSourceRepository>();
        services.AddScoped<IIngestionExecutionRepository, IngestionExecutionRepository>();
        services.AddScoped<IAnalyticsEventRepository, AnalyticsEventRepository>();

        return services;
    }
}
