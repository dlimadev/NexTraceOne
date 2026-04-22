using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Domain;
using NexTraceOne.BuildingBlocks.Application.Integrations;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Contracts;
using NexTraceOne.Integrations.Domain.Events;
using NexTraceOne.Integrations.Infrastructure.CloudBilling;
using NexTraceOne.Integrations.Infrastructure.EventHandlers;
using NexTraceOne.Integrations.Infrastructure.Integrations;
using NexTraceOne.Integrations.Infrastructure.Kafka;
using NexTraceOne.Integrations.Infrastructure.LegacyTelemetry;
using NexTraceOne.Integrations.Application.Services;
using NexTraceOne.Integrations.Application.Services.NormalizationStrategies;
using NexTraceOne.Integrations.Infrastructure.Persistence;
using NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;
using NexTraceOne.Integrations.Infrastructure.Saml;

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

        // Canary Provider — NullCanaryProvider por default; substituir por implementação real quando sistema canary estiver disponível
        services.AddSingleton<ICanaryProvider, NullCanaryProvider>();

        // Backup Provider — NullBackupProvider por default; substituir por implementação real (pg_dump, pgBackRest, Barman)
        // quando sistema de backup estiver configurado no ambiente.
        services.AddSingleton<IBackupProvider, NullBackupProvider>();

        // Kafka Event Producer — activa ConfluentKafkaEventProducer quando Kafka:Enabled = true e BootstrapServers configurados.
        // Caso contrário, usa NullKafkaEventProducer que descarta silenciosamente todos os eventos.
        var kafkaEnabled = configuration.GetValue<bool>("Kafka:Enabled");
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"];
        if (kafkaEnabled && !string.IsNullOrWhiteSpace(kafkaBootstrap))
        {
            services.AddSingleton<IKafkaEventProducer, ConfluentKafkaEventProducer>();
            services.AddHostedService<KafkaConsumerWorker>();
        }
        else
        {
            services.AddSingleton<IKafkaEventProducer, NullKafkaEventProducer>();
        }

        // Cloud Billing Provider — NullCloudBillingProvider por default.
        // Substituir por AwsCloudBillingProvider, AzureCloudBillingProvider, etc.
        // quando FinOps:Billing:Provider estiver configurado.
        services.AddSingleton<ICloudBillingProvider, NullCloudBillingProvider>();

        // SAML Provider — ConfigurationSamlProvider quando Saml:EntityId e Saml:SsoUrl
        // estiverem configurados; NullSamlProvider por defeito (retorna IsConfigured = false).
        var samlEntityId = configuration["Saml:EntityId"];
        var samlSsoUrl = configuration["Saml:SsoUrl"];
        if (!string.IsNullOrWhiteSpace(samlEntityId) && !string.IsNullOrWhiteSpace(samlSsoUrl))
        {
            services.AddSingleton<ISamlProvider, ConfigurationSamlProvider>();
        }
        else
        {
            services.AddSingleton<ISamlProvider, NullSamlProvider>();
        }

        // Event Consumer Worker — Dead Letter Repository e Status Reader (null por defeito)
        services.AddSingleton<IEventConsumerDeadLetterRepository, NullEventConsumerDeadLetterRepository>();
        services.AddSingleton<IEventConsumerStatusReader, NullEventConsumerStatusReader>();

        // Normalization Strategies — registadas como IEventNormalizationStrategy para injecção no worker
        services.AddSingleton<IEventNormalizationStrategy, KafkaChangeEventStrategy>();
        services.AddSingleton<IEventNormalizationStrategy, ServiceBusChangeEventStrategy>();
        services.AddSingleton<IEventNormalizationStrategy, SqsChangeEventStrategy>();
        services.AddSingleton<IEventNormalizationStrategy, RabbitMqChangeEventStrategy>();

        return services;
    }
}
