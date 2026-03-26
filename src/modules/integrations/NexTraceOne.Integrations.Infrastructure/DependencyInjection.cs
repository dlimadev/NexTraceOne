using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Infrastructure.Persistence;
using NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.Integrations.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Integrations.
/// Inclui: DbContext, Repositórios, UnitOfWork.
///
/// Módulo criado em P2.1 para receber IntegrationConnector extraído de Governance.
/// Em P2.2, IngestionSource e IngestionExecution também serão migradas para cá.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Integrations.</summary>
    public static IServiceCollection AddIntegrationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("IntegrationsDatabase", "NexTraceOne");

        services.AddDbContext<IntegrationsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        // Repositories
        services.AddScoped<IIntegrationConnectorRepository, IntegrationConnectorRepository>();

        return services;
    }
}
