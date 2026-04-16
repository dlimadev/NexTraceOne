using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Catalog.Contracts.IntegrationEvents;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.Governance.Contracts;
using NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Events;
using NexTraceOne.Notifications.Infrastructure.Engine;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;
using NexTraceOne.Notifications.Infrastructure.ExternalDelivery;
using NexTraceOne.Notifications.Infrastructure.Governance;
using NexTraceOne.Notifications.Infrastructure.Intelligence;
using NexTraceOne.Notifications.Infrastructure.Persistence;
using NexTraceOne.Notifications.Infrastructure.Persistence.Repositories;
using NexTraceOne.Notifications.Infrastructure.Preferences;
using NexTraceOne.Notifications.Infrastructure.Routing;
// P7.1 – new stores
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;
using IntegrationsContracts = NexTraceOne.Integrations.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Notifications.
/// Inclui engine de deduplicação (Fase 2), event handlers (Fase 2) e
/// canais externos de entrega — Email e Microsoft Teams (Fase 3).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("NotificationsDatabase", "NexTraceOne");

        services.AddDbContext<NotificationsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<INotificationStore, NotificationStoreRepository>();

        // ── P7.1: Templates, Channels e SMTP persistidos ──
        services.AddScoped<INotificationTemplateStore, NotificationTemplateRepository>();
        services.AddScoped<IDeliveryChannelConfigurationStore, DeliveryChannelConfigurationRepository>();
        services.AddScoped<ISmtpConfigurationStore, SmtpConfigurationRepository>();

        // ── Fase 4: Preferências, resolução de destinatários, políticas obrigatórias ──
        services.AddScoped<INotificationPreferenceStore, NotificationPreferenceStoreRepository>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddSingleton<IMandatoryNotificationPolicy, MandatoryNotificationPolicy>();
        services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();

        // Engine — Fase 2: deduplicação básica
        services.AddScoped<INotificationDeduplicationService, NotificationDeduplicationService>();

        // ── Phase 6: Intelligence & Automation ──
        services.AddScoped<INotificationGroupingService, NotificationGroupingService>();
        services.AddSingleton<IQuietHoursService, QuietHoursService>();
        services.AddScoped<INotificationEscalationService, NotificationEscalationService>();
        services.AddScoped<INotificationSuppressionService, NotificationSuppressionService>();
        services.AddScoped<INotificationDigestService, NotificationDigestService>();

        // ── Fase 3: Canais Externos ──

        // Configuração por ambiente
        services.Configure<NotificationChannelOptions>(
            configuration.GetSection(NotificationChannelOptions.SectionName));
        services.Configure<DeliveryRetryOptions>(
            configuration.GetSection(DeliveryRetryOptions.SectionName));

        // Delivery store (persistência de delivery logs)
        services.AddScoped<INotificationDeliveryStore, NotificationDeliveryStoreRepository>();

        // Routing engine (decisão de canais)
        services.AddScoped<INotificationRoutingEngine, NotificationRoutingEngine>();

        // Template resolver para canais externos
        services.AddSingleton<IExternalChannelTemplateResolver, ExternalChannelTemplateResolver>();

        // Channel dispatchers
        services.AddScoped<INotificationChannelDispatcher, EmailNotificationDispatcher>();
        services.AddScoped<INotificationChannelDispatcher, TeamsNotificationDispatcher>();

        // HttpClientFactory para Teams webhook
        services.AddHttpClient("NexTraceOneTeams");

        // External delivery service (coordena roteamento + dispatch + logging)
        services.AddScoped<IExternalDeliveryService, ExternalDeliveryService>();
        // P7.2: Background retry job para deliveries agendados
        services.AddHostedService<NotificationDeliveryRetryJob>();

        // Event Handlers — Fase 2: primeiros eventos automáticos de alto valor
        services.AddScoped<IIntegrationEventHandler<IncidentCreatedIntegrationEvent>, IncidentNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IncidentEscalatedIntegrationEvent>, IncidentNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<ApprovalPendingIntegrationEvent>, ApprovalNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<WorkflowRejectedIntegrationEvent>, ApprovalNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<BreakGlassActivatedIntegrationEvent>, SecurityNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationEvents.ComplianceCheckFailedIntegrationEvent>, ComplianceNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<BudgetExceededIntegrationEvent>, BudgetNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationFailedIntegrationEvent>, IntegrationFailureNotificationHandler>();

        // ── Phase 5: High-Value Domain Event Handlers ──

        // Operations & Incidents
        services.AddScoped<IIntegrationEventHandler<IncidentResolvedIntegrationEvent>, IncidentNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<AnomalyDetectedIntegrationEvent>, IncidentNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<HealthDegradationIntegrationEvent>, IncidentNotificationHandler>();

        // Approvals & Workflow
        services.AddScoped<IIntegrationEventHandler<ApprovalApprovedIntegrationEvent>, ApprovalNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<ApprovalExpiringIntegrationEvent>, ApprovalNotificationHandler>();

        // Catalog & Contracts
        services.AddScoped<IIntegrationEventHandler<ContractPublishedIntegrationEvent>, CatalogNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<BreakingChangeDetectedIntegrationEvent>, CatalogNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<ContractValidationFailedIntegrationEvent>, CatalogNotificationHandler>();

        // Security & Access
        services.AddScoped<IIntegrationEventHandler<UserRoleChangedIntegrationEvent>, SecurityNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<JitAccessGrantedIntegrationEvent>, SecurityNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<AccessReviewPendingIntegrationEvent>, SecurityNotificationHandler>();

        // Governance & Compliance
        services.AddScoped<IIntegrationEventHandler<IntegrationEvents.PolicyViolatedIntegrationEvent>, ComplianceNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationEvents.EvidenceExpiringIntegrationEvent>, ComplianceNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationEvents.BudgetThresholdReachedIntegrationEvent>, ComplianceNotificationHandler>();

        // AI Governance
        services.AddScoped<IIntegrationEventHandler<AiProviderUnavailableIntegrationEvent>, AiGovernanceNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<TokenBudgetExceededIntegrationEvent>, AiGovernanceNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<AiGenerationFailedIntegrationEvent>, AiGovernanceNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<AiActionBlockedByPolicyIntegrationEvent>, AiGovernanceNotificationHandler>();

        // Integrations & Ingestion
        // P2.5: SyncFailedIntegrationEvent and ConnectorAuthFailedIntegrationEvent now from Integrations.Contracts
        //       (ownership corrected from OperationalIntelligence.Contracts).
        services.AddScoped<IIntegrationEventHandler<IntegrationsContracts.SyncFailedIntegrationEvent>, IntegrationFailureNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationsContracts.ConnectorAuthFailedIntegrationEvent>, IntegrationFailureNotificationHandler>();

        // ── Phase 7: Metrics, Audit & Governance ──
        services.AddScoped<INotificationMetricsService, NotificationMetricsService>();
        services.AddScoped<INotificationAuditService, NotificationAuditService>();
        services.AddScoped<INotificationHealthProvider, NotificationHealthProvider>();
        services.AddScoped<INotificationCatalogGovernance, NotificationCatalogGovernance>();

        // ── Domain Event Handlers (via Notifications Outbox → IEventBus) ─────────────
        // NOTA: O processador do outbox do módulo Notifications (NotificationsOutboxProcessorJob)
        // está pendente de implementação. Quando implementado, estes handlers serão invocados
        // automaticamente via IEventBus.PublishAsync<T> para cada evento pendente no outbox.
        services.AddScoped<IIntegrationEventHandler<NotificationCreatedEvent>, NotificationCreatedDomainEventHandler>();
        services.AddScoped<IIntegrationEventHandler<NotificationReadEvent>, NotificationReadDomainEventHandler>();

        return services;
    }
}
