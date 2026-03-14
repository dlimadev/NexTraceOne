using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.InProcess;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.BuildingBlocks.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura compartilhados: Interceptors, Converters, Outbox, EventBus.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registra interceptors, converters e demais serviços de infraestrutura.</summary>
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TenantRlsInterceptor>();

        return services;
    }

    /// <summary>Registra o barramento de eventos compartilhado da plataforma.</summary>
    public static IServiceCollection AddBuildingBlocksEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IEventBus, InProcessEventBus>();

        return services;
    }
}
