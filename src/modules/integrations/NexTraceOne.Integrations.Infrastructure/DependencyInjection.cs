using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Integrations;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Contracts;
using NexTraceOne.Integrations.Domain.Events;
using NexTraceOne.Integrations.Infrastructure.EventHandlers;
using NexTraceOne.Integrations.Infrastructure.Integrations;
using NexTraceOne.Integrations.Infrastructure.LegacyTelemetry;
using NexTraceOne.Integrations.Infrastructure.Persistence;
using NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.Integrations.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Integrations.
/// Inclui: DbContext, Repositórios, UnitOfWork.
///
/// P2.1: IntegrationConnector extraído de Governance.
/// P2.2: IngestionSource e IngestionExecution extraídos de Governance.
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

        // Repositories — P2.1
        services.AddScoped<IIntegrationConnectorRepository, IntegrationConnectorRepository>();

        // Repositories — P2.2
        services.AddScoped<IIngestionSourceRepository, IngestionSourceRepository>();
        services.AddScoped<IIngestionExecutionRepository, IngestionExecutionRepository>();

        // Repositories — Webhook Subscriptions
        services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();

        // Legacy Telemetry — Elastic writer (padrão)
        services.Configure<ElasticLegacyWriterOptions>(
            configuration.GetSection(ElasticLegacyWriterOptions.SectionName));
        services.AddHttpClient<ILegacyEventWriter, ElasticLegacyEventWriter>();

        // Integration Context Resolver — resolves active binding descriptors by type, tenant and environment
        services.AddScoped<IIntegrationContextResolver, IntegrationContextResolver>();

        // Domain Event Handlers — converte domain events em integration events para consumidores downstream
        services.AddScoped<IIntegrationEventHandler<IngestionPayloadProcessedDomainEvent>,
            IngestionPayloadProcessedDomainEventHandler>();

        return services;
    }
}
