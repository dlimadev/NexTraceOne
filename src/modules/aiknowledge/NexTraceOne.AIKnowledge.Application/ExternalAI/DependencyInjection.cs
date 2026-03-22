using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ApproveKnowledgeCapture;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.CaptureExternalAIResponse;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ConfigureExternalAIPolicy;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.GetExternalAIUsage;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ListKnowledgeCaptures;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAIAdvanced;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAISimple;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ReuseKnowledgeCapture;
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
        services.AddTransient<IValidator<CaptureExternalAIResponse.Command>, CaptureExternalAIResponse.Validator>();
        services.AddTransient<IValidator<ApproveKnowledgeCapture.Command>, ApproveKnowledgeCapture.Validator>();
        services.AddTransient<IValidator<ListKnowledgeCaptures.Query>, ListKnowledgeCaptures.Validator>();
        services.AddTransient<IValidator<GetExternalAIUsage.Query>, GetExternalAIUsage.Validator>();
        services.AddTransient<IValidator<ReuseKnowledgeCapture.Command>, ReuseKnowledgeCapture.Validator>();
        services.AddTransient<IValidator<ConfigureExternalAIPolicy.Command>, ConfigureExternalAIPolicy.Validator>();
        return services;
    }
}
