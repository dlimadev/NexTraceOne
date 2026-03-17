using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AuditCompliance.Application;
using NexTraceOne.AuditCompliance.Infrastructure;

namespace NexTraceOne.AuditCompliance.API.Endpoints;

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
