using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using AskCatalogQuestionFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.AskCatalogQuestion.AskCatalogQuestion;
using ClassifyChangeWithAIFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.ClassifyChangeWithAI.ClassifyChangeWithAI;
using SuggestSemanticVersionWithAIFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.SuggestSemanticVersionWithAI.SuggestSemanticVersionWithAI;
using AnalyzeNonProdEnvironmentFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.AnalyzeNonProdEnvironment.AnalyzeNonProdEnvironment;
using CompareEnvironmentsFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.CompareEnvironments.CompareEnvironments;
using AssessPromotionReadinessFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.AssessPromotionReadiness.AssessPromotionReadiness;
using GetAiConversationHistoryFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAiConversationHistory.GetAiConversationHistory;
using ValidateKnowledgeCaptureFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.ValidateKnowledgeCapture.ValidateKnowledgeCapture;
using GenerateTestScenariosFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateTestScenarios.GenerateTestScenarios;
using GenerateRobotFrameworkDraftFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateRobotFrameworkDraft.GenerateRobotFrameworkDraft;
using SummarizeReleaseForApprovalFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.SummarizeReleaseForApproval.SummarizeReleaseForApproval;
using GenerateAiScaffoldFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateAiScaffold.GenerateAiScaffold;
using EvaluateArchitectureFitnessFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.EvaluateArchitectureFitness.EvaluateArchitectureFitness;
using EvaluateDocumentationQualityFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.EvaluateDocumentationQuality.EvaluateDocumentationQuality;
using GenerateAdrFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateArchitectureDecisionRecord.GenerateArchitectureDecisionRecord;
using RecommendTemplateFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.RecommendTemplateForService.RecommendTemplateForService;
using ReviewContractDraftFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.ReviewContractDraft.ReviewContractDraft;
using GetAgentMarketplaceFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAgentMarketplace.GetAgentMarketplace;

namespace NexTraceOne.AIKnowledge.API.Orchestration.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo AiOrchestration.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Política de autorização:
/// - Escrita: "ai:runtime:write" para endpoints de execução de orquestração de IA.
/// - Leitura: "ai:runtime:read" para histórico e validação.
/// </summary>
public sealed class AiOrchestrationEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapCatalogEndpoints(app);
        MapChangeEndpoints(app);
        MapContractEndpoints(app);
        MapEnvironmentAnalysisEndpoints(app);
        MapConversationEndpoints(app);
        MapKnowledgeEndpoints(app);
        MapGenerationEndpoints(app);
        MapMarketplaceEndpoints(app);
    }

    private static void MapCatalogEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/catalog").RequireRateLimiting("ai");

        group.MapPost("/ask", async (
            AskCatalogQuestionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapChangeEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/changes").RequireRateLimiting("ai");

        group.MapPost("/classify", async (
            ClassifyChangeWithAIFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapContractEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/contracts").RequireRateLimiting("ai");

        group.MapPost("/suggest-version", async (
            SuggestSemanticVersionWithAIFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/review", async (
            ReviewContractDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapEnvironmentAnalysisEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/analysis").RequireRateLimiting("ai");

        group.MapPost("/non-prod", async (
            AnalyzeNonProdEnvironmentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/compare-environments", async (
            CompareEnvironmentsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/promotion-readiness", async (
            AssessPromotionReadinessFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapConversationEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/conversations");

        group.MapGet("/history", async (
            Guid? releaseId,
            string? serviceName,
            string? topicFilter,
            ConversationStatus? status,
            DateTimeOffset? from,
            DateTimeOffset? to,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new GetAiConversationHistoryFeature.Query(
                releaseId, serviceName, topicFilter, status, from, to,
                page == 0 ? 1 : page,
                pageSize == 0 ? 20 : pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");
    }

    private static void MapKnowledgeEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/knowledge");

        group.MapPost("/entries/{entryId:guid}/validate", async (
            Guid entryId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ValidateKnowledgeCaptureFeature.Command(entryId);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapGenerationEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/generate").RequireRateLimiting("ai");

        group.MapPost("/test-scenarios", async (
            GenerateTestScenariosFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/robot-framework", async (
            GenerateRobotFrameworkDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        // ── POST /api/v1/aiorchestration/generate/scaffold — AI Scaffold Generation ──
        group.MapPost("/scaffold", async (
            AiScaffoldRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new GenerateAiScaffoldFeature.Command(
                TemplateId: request.TemplateId,
                TemplateSlug: request.TemplateSlug,
                ServiceName: request.ServiceName,
                ServiceDescription: request.ServiceDescription,
                TeamName: request.TeamName,
                Domain: request.Domain,
                LanguageOverride: request.LanguageOverride,
                MainEntities: request.MainEntities,
                AdditionalRequirements: request.AdditionalRequirements,
                PreferredProvider: request.PreferredProvider);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("ai:runtime:write")
        .WithName("GenerateAiScaffold");

        group.MapPost("/releases/{releaseId:guid}/approval-summary", async (
            Guid releaseId,
            SummarizeReleaseForApprovalRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new SummarizeReleaseForApprovalFeature.Command(
                releaseId,
                req.ReleaseName,
                req.Scope,
                req.ImpactedServices,
                req.KnownRisks,
                req.AdditionalContext,
                req.PreferredProvider);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        // ── POST /api/v1/aiorchestration/generate/architecture-fitness — Phase 6 AI Quality Gates ──
        group.MapPost("/architecture-fitness", async (
            ArchitectureFitnessRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new EvaluateArchitectureFitnessFeature.Command(
                req.TargetId,
                req.ServiceName,
                req.Files.Select(f => new EvaluateArchitectureFitnessFeature.CodeFile(f.FileName, f.Content)).ToList(),
                req.PreferredProvider);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        // ── POST /api/v1/aiorchestration/generate/documentation-quality — Phase 6 AI Quality Gates ──
        group.MapPost("/documentation-quality", async (
            DocumentationQualityRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new EvaluateDocumentationQualityFeature.Command(
                req.ServiceName,
                req.Files.Select(f => new EvaluateDocumentationQualityFeature.DocumentFile(f.FileName, f.Content, f.FileType)).ToList(),
                req.PreferredProvider);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        // ── POST /api/v1/aiorchestration/generate/adr — Phase 7 ADR Generator ──
        group.MapPost("/adr", async (
            AdrRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new GenerateAdrFeature.Command(
                req.ServiceName,
                req.DecisionContext,
                req.ArchitectureStyle,
                req.TechStack,
                req.SelectedTemplate,
                req.PreferredProvider);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        // ── POST /api/v1/aiorchestration/recommend-template — Phase 7 Smart Template Recommendations ──
        group.MapPost("/recommend-template", async (
            RecommendTemplateRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new RecommendTemplateFeature.Command(
                req.ServiceDescription,
                req.PreferredLanguage,
                req.Domain,
                req.TeamName,
                req.AvailableTemplates.Select(t => new RecommendTemplateFeature.TemplateInfo(t.Slug, t.DisplayName, t.Description, t.ServiceType, t.PrimaryLanguage, t.Tags)).ToList(),
                req.MaxRecommendations,
                req.PreferredProvider);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapMarketplaceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration");

        group.MapGet("/marketplace", async (
            string? category,
            string? search,
            bool? isOfficial,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new GetAgentMarketplaceFeature.Query(category, search, isOfficial, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");
    }

    private sealed record AdrRequest(
        string ServiceName,
        string DecisionContext,
        string? ArchitectureStyle = null,
        string? TechStack = null,
        string? SelectedTemplate = null,
        string? PreferredProvider = null);

    private sealed record RecommendTemplateInfo(string Slug, string DisplayName, string Description, string ServiceType, string PrimaryLanguage, IReadOnlyList<string> Tags);
    private sealed record RecommendTemplateRequest(
        string ServiceDescription,
        string? PreferredLanguage,
        string? Domain,
        string? TeamName,
        IReadOnlyList<RecommendTemplateInfo> AvailableTemplates,
        int MaxRecommendations = 3,
        string? PreferredProvider = null);

    private sealed record ArchitectureFitnessCodeFile(string FileName, string Content);
    private sealed record ArchitectureFitnessRequest(
        Guid? TargetId,
        string ServiceName,
        IReadOnlyList<ArchitectureFitnessCodeFile> Files,
        string? PreferredProvider);

    private sealed record DocumentationQualityFileEntry(string FileName, string Content, string FileType);
    private sealed record DocumentationQualityRequest(
        string ServiceName,
        IReadOnlyList<DocumentationQualityFileEntry> Files,
        string? PreferredProvider);

    private sealed record AiScaffoldRequest(
        Guid? TemplateId,
        string? TemplateSlug,
        string ServiceName,
        string ServiceDescription,
        string? TeamName,
        string? Domain,
        string? LanguageOverride,
        string? MainEntities,
        string? AdditionalRequirements,
        string? PreferredProvider);

    private sealed record SummarizeReleaseForApprovalRequest(
        string ReleaseName,
        string? Scope,
        IReadOnlyList<string>? ImpactedServices,
        IReadOnlyList<string>? KnownRisks,
        string? AdditionalContext,
        string? PreferredProvider);
}
