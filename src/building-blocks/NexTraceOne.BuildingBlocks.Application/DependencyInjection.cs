using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Behaviors;
using NexTraceOne.BuildingBlocks.Application.Correlation;
using NexTraceOne.BuildingBlocks.Application.Integrations;
using NexTraceOne.BuildingBlocks.Application.Localization;

namespace NexTraceOne.BuildingBlocks.Application;

/// <summary>
/// Registra serviços do BuildingBlocks.Application no DI.
/// Inclui: Pipeline Behaviors MediatR, DateTimeProvider, validators.
/// Método idempotente — seguro para ser chamado por múltiplos módulos.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services.Any(d => d.ImplementationType == typeof(ValidationBehavior<,>)))
            return services;

        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.AddScoped<IErrorLocalizer, ErrorLocalizer>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ContextualLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantIsolationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // Fase 5: Contexto distribuído — integração e correlação
        services.AddScoped<IIntegrationContextResolver, NullIntegrationContextResolver>();
        services.AddScoped<IDistributedSignalCorrelationService, NullDistributedSignalCorrelationService>();
        services.AddScoped<IPromotionRiskSignalProvider, NullPromotionRiskSignalProvider>();

        return services;
    }
}
