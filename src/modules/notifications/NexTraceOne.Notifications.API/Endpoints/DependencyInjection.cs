using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Notifications.Application;
using NexTraceOne.Notifications.Infrastructure;

namespace NexTraceOne.Notifications.API.Endpoints;

/// <summary>
/// Registra serviços específicos da camada API do módulo Notifications.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddNotificationsApplication(configuration);
        services.AddNotificationsInfrastructure(configuration);
        return services;
    }
}
