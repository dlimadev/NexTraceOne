using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Engine;
using NexTraceOne.Notifications.Application.Features.AcknowledgeNotification;
using NexTraceOne.Notifications.Application.Features.ArchiveNotification;
using NexTraceOne.Notifications.Application.Features.DismissNotification;
using NexTraceOne.Notifications.Application.Features.GetNotificationDeliveryReport;
using NexTraceOne.Notifications.Application.Features.GetNotificationEffectivenessReport;
using NexTraceOne.Notifications.Application.Features.ListNotifications;
using NexTraceOne.Notifications.Application.Features.MarkNotificationRead;
using NexTraceOne.Notifications.Application.Features.MarkNotificationUnread;
using NexTraceOne.Notifications.Application.Features.SnoozeNotification;
using NexTraceOne.Notifications.Application.Features.UpdatePreference;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.Notifications.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Notifications.
/// Inclui a engine de notificações automáticas (Fase 2).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<ListNotifications.Query>, ListNotifications.Validator>();
        services.AddTransient<IValidator<MarkNotificationRead.Command>, MarkNotificationRead.Validator>();
        services.AddTransient<IValidator<MarkNotificationUnread.Command>, MarkNotificationUnread.Validator>();
        services.AddTransient<IValidator<UpdatePreference.Command>, UpdatePreference.Validator>();
        services.AddTransient<IValidator<AcknowledgeNotification.Command>, AcknowledgeNotification.Validator>();
        services.AddTransient<IValidator<ArchiveNotification.Command>, ArchiveNotification.Validator>();
        services.AddTransient<IValidator<DismissNotification.Command>, DismissNotification.Validator>();
        services.AddTransient<IValidator<SnoozeNotification.Command>, SnoozeNotification.Validator>();
        services.AddTransient<IValidator<GetNotificationDeliveryReport.Query>, GetNotificationDeliveryReport.Validator>();
        services.AddTransient<IValidator<GetNotificationEffectivenessReport.Query>, GetNotificationEffectivenessReport.Validator>();

        // Engine de notificações — Fase 2
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
        services.AddSingleton<INotificationTemplateResolver, NotificationTemplateResolver>();
        services.AddScoped<INotificationModule, NotificationModuleService>();

        return services;
    }
}
