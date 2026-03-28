using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.ClassifyChangeWithAI;

/// <summary>
/// Feature: ClassifyChangeWithAI — classifica uma mudança usando IA e sugere mitigação.
/// Constrói grounding a partir dos dados da mudança, chama o provider e faz parse da resposta.
/// </summary>
public static class ClassifyChangeWithAI
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    public sealed record Command(
        string ChangeTitle,
        string? ChangeDescription,
        string? AffectedService,
        string? CurrentVersion,
        string? TargetVersion,
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChangeTitle).NotEmpty();
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IExternalAIRoutingPort externalAiRoutingPort,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.ChangeTitle);

            var correlationId = Guid.NewGuid().ToString();
            var groundingContext = BuildGroundingContext(request);
            const string query =
                "Classify this change as one of: Breaking, NonBreaking, Patch, Feature, Security, Configuration. " +
                "Then list mitigation steps starting each step with '- '. " +
                "Be concise and structured.";

            string content;
            try
            {
                content = await externalAiRoutingPort.RouteQueryAsync(
                    groundingContext,
                    query,
                    request.PreferredProvider,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for change classification. CorrelationId={CorrelationId}", correlationId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var isFallback = content.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            var suggestedChangeType = ParseChangeType(content);
            var mitigationSteps = ParseMitigationSteps(content);

            return Result<Response>.Success(new Response(content, suggestedChangeType, mitigationSteps, isFallback, correlationId));
        }

        private static string BuildGroundingContext(Command request)
        {
            var lines = new List<string>
            {
                $"Change title: {request.ChangeTitle}"
            };

            if (!string.IsNullOrWhiteSpace(request.ChangeDescription))
                lines.Add($"Description: {request.ChangeDescription}");

            if (!string.IsNullOrWhiteSpace(request.AffectedService))
                lines.Add($"Affected service: {request.AffectedService}");

            if (!string.IsNullOrWhiteSpace(request.CurrentVersion))
                lines.Add($"Current version: {request.CurrentVersion}");

            if (!string.IsNullOrWhiteSpace(request.TargetVersion))
                lines.Add($"Target version: {request.TargetVersion}");

            return string.Join("\n", lines);
        }

        private static string ParseChangeType(string content)
        {
            var lower = content.ToLowerInvariant();

            if (lower.Contains("breaking"))
                return "BreakingChange";
            if (lower.Contains("security"))
                return "Security";
            if (lower.Contains("patch") || lower.Contains("fix"))
                return "Patch";
            if (lower.Contains("feature") || lower.Contains("minor"))
                return "Feature";

            return "Configuration";
        }

        private static IReadOnlyList<string> ParseMitigationSteps(string content)
        {
            var steps = new List<string>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Match lines starting with "- ", "* ", or numbered like "1. ", "2) "
                if (trimmed.StartsWith("- ", StringComparison.Ordinal) ||
                    trimmed.StartsWith("* ", StringComparison.Ordinal) ||
                    Regex.IsMatch(trimmed, @"^\d+[\.\)]\s"))
                {
                    var step = Regex.Replace(trimmed, @"^[-*\d]+[\.\)]*\s*", string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(step))
                        steps.Add(step);
                }
            }

            return steps.AsReadOnly();
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    public sealed record Response(
        string ClassificationRaw,
        string SuggestedChangeType,
        IReadOnlyList<string> SuggestedMitigationSteps,
        bool IsFallback,
        string CorrelationId);
}
