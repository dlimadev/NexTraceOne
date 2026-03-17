using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.IdentityAccess.Application;
using NexTraceOne.IdentityAccess.Infrastructure;

namespace NexTraceOne.IdentityAccess.API.Endpoints;

/// <summary>
/// Registra serviços específicos da camada API do módulo Identity.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentityApplication(configuration);
        services.AddIdentityInfrastructure(configuration);
        return services;
    }
}
