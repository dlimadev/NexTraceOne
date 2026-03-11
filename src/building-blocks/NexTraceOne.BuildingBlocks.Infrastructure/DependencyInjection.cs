using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.BuildingBlocks.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura compartilhados: Interceptors, Converters, Outbox.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TenantRlsInterceptor>();

        return services;
    }
}
