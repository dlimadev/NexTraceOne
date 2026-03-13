using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.Audit.Application;
using NexTraceOne.Audit.Infrastructure;

namespace NexTraceOne.Audit.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo Audit.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuditModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuditApplication(configuration);
        services.AddAuditInfrastructure(configuration);
        return services;
    }
}
