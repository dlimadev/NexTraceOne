using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.EventBus;

/// <summary>Registra implementação do EventBus (InProcess ou Outbox).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar IEventBus (InProcessEventBus ou OutboxEventBus conforme config)
        return services;
    }
}
