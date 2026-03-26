using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence.Repositories;
using NexTraceOne.Integrations.Infrastructure;
using NexTraceOne.ProductAnalytics.Infrastructure;

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

        var connectionString = configuration.GetRequiredConnectionString("GovernanceDatabase", "NexTraceOne");

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
        // NOTE: IIntegrationConnectorRepository removed from Governance in P2.1.
        //       Registered via AddIntegrationsInfrastructure below.
        // NOTE: IAnalyticsEventRepository removed from Governance in P2.3.
        //       Registered via AddProductAnalyticsInfrastructure below.
        // NOTE: IGovernanceAnalyticsRepository is a legitimate Governance repository:
        //       it queries Governance entities (Waivers, Packs, RolloutRecords) for executive trends.
        services.AddScoped<IGovernanceAnalyticsRepository, GovernanceAnalyticsRepository>();

        // COMPATIBILIDADE TRANSITÓRIA (P2.4):
        // Integrations module infrastructure is wired from here because integration handlers
        // (ListIntegrationConnectors, GetIngestionHealth, etc.) remain temporarily in Governance.Application.
        // These handlers already consume Integrations.Application.Abstractions correctly.
        // Full separation into Integrations.API and Integrations.Application is pending.
        services.AddIntegrationsInfrastructure(configuration);

        // COMPATIBILIDADE TRANSITÓRIA (P2.4):
        // Product Analytics module infrastructure is wired from here because analytics handlers
        // (RecordAnalyticsEvent, GetAnalyticsSummary, etc.) remain temporarily in Governance.Application.
        // These handlers already consume ProductAnalytics.Application.Abstractions correctly.
        // Full separation into ProductAnalytics.API and ProductAnalytics.Application is pending.
        services.AddProductAnalyticsInfrastructure(configuration);

        return services;
    }
}
