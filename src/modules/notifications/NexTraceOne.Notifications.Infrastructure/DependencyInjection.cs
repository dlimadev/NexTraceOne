using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.Governance.Contracts;
using NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Infrastructure.Engine;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;
using NexTraceOne.Notifications.Infrastructure.Persistence;
using NexTraceOne.Notifications.Infrastructure.Persistence.Repositories;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Notifications.
/// Inclui engine de deduplicação e event handlers da Fase 2.
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

        // Engine — Fase 2: deduplicação básica
        services.AddScoped<INotificationDeduplicationService, NotificationDeduplicationService>();

        // Event Handlers — Fase 2: primeiros eventos automáticos de alto valor
        services.AddScoped<IIntegrationEventHandler<IncidentCreatedIntegrationEvent>, IncidentNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IncidentEscalatedIntegrationEvent>, IncidentNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<ApprovalPendingIntegrationEvent>, ApprovalNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<WorkflowRejectedIntegrationEvent>, ApprovalNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<BreakGlassActivatedIntegrationEvent>, SecurityNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationEvents.ComplianceCheckFailedIntegrationEvent>, ComplianceNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<BudgetExceededIntegrationEvent>, BudgetNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationFailedIntegrationEvent>, IntegrationFailureNotificationHandler>();

        return services;
    }
}
