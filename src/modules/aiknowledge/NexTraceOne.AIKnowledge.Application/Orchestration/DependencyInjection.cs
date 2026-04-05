using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AnalyzeNonProdEnvironment;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AssessPromotionReadiness;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AskCatalogQuestion;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.ClassifyChangeWithAI;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.CompareEnvironments;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateAiScaffold;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateRobotFrameworkDraft;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateTestScenarios;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAiConversationHistory;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.SuggestSemanticVersionWithAI;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.SummarizeReleaseForApproval;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.ValidateKnowledgeCapture;
using NexTraceOne.BuildingBlocks.Application;

namespace NexTraceOne.AIKnowledge.Application.Orchestration;

/// <summary>
/// Registra serviços da camada Application do módulo AiOrchestration.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiOrchestrationApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddTransient<IValidator<AskCatalogQuestion.Command>, AskCatalogQuestion.Validator>();
        services.AddTransient<IValidator<ClassifyChangeWithAI.Command>, ClassifyChangeWithAI.Validator>();
        services.AddTransient<IValidator<SuggestSemanticVersionWithAI.Command>, SuggestSemanticVersionWithAI.Validator>();
        services.AddTransient<IValidator<AnalyzeNonProdEnvironment.Command>, AnalyzeNonProdEnvironment.Validator>();
        services.AddTransient<IValidator<CompareEnvironments.Command>, CompareEnvironments.Validator>();
        services.AddTransient<IValidator<AssessPromotionReadiness.Command>, AssessPromotionReadiness.Validator>();
        services.AddTransient<IValidator<GetAiConversationHistory.Query>, GetAiConversationHistory.Validator>();
        services.AddTransient<IValidator<ValidateKnowledgeCapture.Command>, ValidateKnowledgeCapture.Validator>();
        services.AddTransient<IValidator<GenerateTestScenarios.Command>, GenerateTestScenarios.Validator>();
        services.AddTransient<IValidator<GenerateRobotFrameworkDraft.Command>, GenerateRobotFrameworkDraft.Validator>();
        services.AddTransient<IValidator<SummarizeReleaseForApproval.Command>, SummarizeReleaseForApproval.Validator>();
        services.AddTransient<IValidator<GenerateAiScaffold.Command>, GenerateAiScaffold.Validator>();
        return services;
    }
}
