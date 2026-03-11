using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        // TODO: Registrar interceptors, converters, outbox processor
        return services;
    }
}
