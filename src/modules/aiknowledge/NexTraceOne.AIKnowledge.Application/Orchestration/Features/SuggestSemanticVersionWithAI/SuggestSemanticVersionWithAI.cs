using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.SuggestSemanticVersionWithAI;

/// <summary>
/// Feature: SuggestSemanticVersionWithAI — sugere versão semântica para uma mudança de contrato ou serviço.
/// Chama o provider de IA com contexto da mudança e faz parse da versão sugerida no formato major.minor.patch.
/// </summary>
public static class SuggestSemanticVersionWithAI
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    public sealed record Command(
        string ContractName,
        string? CurrentVersion,
        string? ChangeDescription,
        string? ChangeType,
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractName).NotEmpty();
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IExternalAIRoutingPort externalAiRoutingPort,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        private static readonly Regex VersionPattern = new(@"\d+\.\d+\.\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.ContractName);

            var correlationId = Guid.NewGuid().ToString();
            var groundingContext = BuildGroundingContext(request);
            const string query =
                "Suggest a semantic version (major.minor.patch) for this change and explain your rationale briefly.";

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
                logger.LogWarning(ex, "AI provider unavailable for version suggestion. CorrelationId={CorrelationId}", correlationId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var isFallback = content.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            var suggestedVersion = ParseVersion(content, request.CurrentVersion);

            return Result<Response>.Success(new Response(suggestedVersion, content, isFallback, correlationId));
        }

        private static string BuildGroundingContext(Command request)
        {
            var lines = new List<string>
            {
                $"Contract or service name: {request.ContractName}"
            };

            if (!string.IsNullOrWhiteSpace(request.CurrentVersion))
                lines.Add($"Current version: {request.CurrentVersion}");

            if (!string.IsNullOrWhiteSpace(request.ChangeType))
                lines.Add($"Change type: {request.ChangeType}");

            if (!string.IsNullOrWhiteSpace(request.ChangeDescription))
                lines.Add($"Change description: {request.ChangeDescription}");

            return string.Join("\n", lines);
        }

        private static string ParseVersion(string content, string? currentVersion)
        {
            var match = VersionPattern.Match(content);
            if (match.Success)
                return match.Value;

            // Fallback: increment patch of current version if available
            if (!string.IsNullOrWhiteSpace(currentVersion))
            {
                var currentMatch = VersionPattern.Match(currentVersion);
                if (currentMatch.Success)
                {
                    var parts = currentMatch.Value.Split('.');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var patch))
                        return $"{parts[0]}.{parts[1]}.{patch + 1}";
                }
            }

            return "1.0.0";
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    public sealed record Response(
        string SuggestedVersion,
        string Rationale,
        bool IsFallback,
        string CorrelationId);
}
