using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAIAdvanced;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAISimple;
using NexTraceOne.BuildingBlocks.Application;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI;

/// <summary>
/// Registra serviços da camada Application do módulo ExternalAi.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddExternalAiApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddTransient<IValidator<QueryExternalAISimple.Command>, QueryExternalAISimple.Validator>();
        services.AddTransient<IValidator<QueryExternalAIAdvanced.Command>, QueryExternalAIAdvanced.Validator>();
        return services;
    }
}
