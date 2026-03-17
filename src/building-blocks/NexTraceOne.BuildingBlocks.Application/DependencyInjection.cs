using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Behaviors;
using NexTraceOne.BuildingBlocks.Application.Localization;

namespace NexTraceOne.BuildingBlocks.Application;

/// <summary>
/// Registra serviços do BuildingBlocks.Application no DI.
/// Inclui: Pipeline Behaviors MediatR, DateTimeProvider, validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.AddScoped<IErrorLocalizer, ErrorLocalizer>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantIsolationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        return services;
    }
}
