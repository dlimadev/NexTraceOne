using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Notifications.Application.Features.ListNotifications;
using NexTraceOne.Notifications.Application.Features.MarkNotificationRead;
using NexTraceOne.Notifications.Application.Features.MarkNotificationUnread;

namespace NexTraceOne.Notifications.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Notifications.
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

        return services;
    }
}
