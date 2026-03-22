using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.SummarizeReleaseForApproval;

/// <summary>
/// Feature: SummarizeReleaseForApproval — gera resumo executivo/técnico de release para
/// apoio à aprovação, usando dados reais da plataforma (conversas de IA, artefatos gerados,
/// contexto fornecido) e o provider de IA via IExternalAIRoutingPort.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SummarizeReleaseForApproval
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>Comando para gerar resumo de release para suporte à aprovação.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string ReleaseName,
        string? Scope,
        IReadOnlyList<string>? ImpactedServices,
        IReadOnlyList<string>? KnownRisks,
        string? AdditionalContext,
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ReleaseName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Scope).MaximumLength(5_000).When(x => x.Scope is not null);
            RuleFor(x => x.AdditionalContext).MaximumLength(5_000).When(x => x.AdditionalContext is not null);
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IExternalAIRoutingPort routingPort,
        IAiOrchestrationConversationRepository conversationRepository,
        IGeneratedTestArtifactRepository artifactRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            // Buscar dados reais da release nos repositórios de orquestração
            var conversations = await conversationRepository.GetRecentByReleaseAsync(
                request.ReleaseId, maxCount: 5, cancellationToken);

            var artifacts = await artifactRepository.GetRecentByReleaseAsync(
                request.ReleaseId, maxCount: 10, cancellationToken);

            var prompt = BuildPrompt(request, conversations, artifacts);
            var context = $"release-approval:{request.ReleaseId}";

            string aiSummary;
            bool isFallback;

            try
            {
                aiSummary = await routingPort.RouteQueryAsync(
                    context,
                    prompt,
                    request.PreferredProvider,
                    cancellationToken);

                isFallback = aiSummary.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for SummarizeReleaseForApproval. ReleaseId={ReleaseId}", request.ReleaseId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var confidenceIndicators = new List<string>();
            if (conversations.Count > 0)
                confidenceIndicators.Add($"{conversations.Count} AI conversation(s) analysed for this release.");
            if (artifacts.Count > 0)
                confidenceIndicators.Add($"{artifacts.Count} generated test artifact(s) available.");
            if (isFallback)
                confidenceIndicators.Add("Warning: AI provider was unavailable — summary may be incomplete.");

            var limitations = new List<string>();
            if (conversations.Count == 0 && artifacts.Count == 0)
                limitations.Add("No AI conversation history or test artifacts found for this release. Summary based solely on provided metadata.");

            logger.LogInformation(
                "Release summary generated for '{ReleaseName}' (ReleaseId={ReleaseId}). Conversations={ConvCount}, Artifacts={ArtCount}, IsFallback={IsFallback}",
                request.ReleaseName, request.ReleaseId, conversations.Count, artifacts.Count, isFallback);

            return new Response(
                request.ReleaseId,
                request.ReleaseName,
                aiSummary,
                conversations.Count,
                artifacts.Count,
                confidenceIndicators,
                limitations,
                isFallback,
                now);
        }

        private static string BuildPrompt(
            Command request,
            IReadOnlyList<ConversationSummaryData> conversations,
            IReadOnlyList<ArtifactSummaryData> artifacts)
        {
            var parts = new List<string>
            {
                "Generate a concise executive and technical summary for release approval.",
                $"Release: {request.ReleaseName} (ID: {request.ReleaseId})"
            };

            if (!string.IsNullOrWhiteSpace(request.Scope))
                parts.Add($"Scope:\n{request.Scope}");

            if (request.ImpactedServices?.Count > 0)
                parts.Add($"Impacted services:\n- {string.Join("\n- ", request.ImpactedServices)}");

            if (request.KnownRisks?.Count > 0)
                parts.Add($"Known risks:\n- {string.Join("\n- ", request.KnownRisks)}");

            if (conversations.Count > 0)
            {
                parts.Add("AI conversation context for this release:");
                foreach (var c in conversations)
                    parts.Add($"  - Topic: {c.Topic} ({c.TurnCount} turns, Status: {c.Status})" +
                               (string.IsNullOrWhiteSpace(c.Summary) ? "" : $" — {c.Summary}"));
            }

            if (artifacts.Count > 0)
            {
                parts.Add("Generated test artifacts:");
                foreach (var a in artifacts)
                    parts.Add($"  - {a.ServiceName} / {a.TestFramework} (Status: {a.Status}, Confidence: {a.Confidence:P0})");
            }

            if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
                parts.Add($"Additional context:\n{request.AdditionalContext}");

            parts.Add("Include: executive summary, key changes, impacted areas, risk assessment, test coverage, approval recommendation. Be concise and actionable.");

            return string.Join("\n\n", parts);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resultado da sumarização de release para aprovação.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string ReleaseName,
        string Summary,
        int AnalysedConversationsCount,
        int AnalysedArtifactsCount,
        IReadOnlyList<string> ConfidenceIndicators,
        IReadOnlyList<string> Limitations,
        bool IsFallback,
        DateTimeOffset GeneratedAt);
}
