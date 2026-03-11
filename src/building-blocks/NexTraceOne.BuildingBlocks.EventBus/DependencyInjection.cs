using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.EventBus.InProcess;

namespace NexTraceOne.BuildingBlocks.EventBus;

/// <summary>Registra implementação do EventBus (InProcess ou Outbox).</summary>
public static class DependencyInjection
{
    /// <summary>Registra o barramento de eventos compartilhado da plataforma.</summary>
    public static IServiceCollection AddBuildingBlocksEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IEventBus, InProcessEventBus>();

        return services;
    }
}
