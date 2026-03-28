using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.CompareEnvironments;

/// <summary>
/// Feature: CompareEnvironments — A IA compara dois ambientes do mesmo tenant e destaca divergências,
/// regressões e sinais relevantes para decisão de promoção.
///
/// REGRA: A comparação é sempre dentro do MESMO tenant. Não é possível comparar ambientes
/// de tenants diferentes — o backend garante este isolamento.
/// </summary>
public static class CompareEnvironments
{
    public sealed record Command(
        string TenantId,
        string SubjectEnvironmentId,
        string SubjectEnvironmentName,
        string SubjectEnvironmentProfile,
        string ReferenceEnvironmentId,
        string ReferenceEnvironmentName,
        string ReferenceEnvironmentProfile,
        IReadOnlyList<string>? ServiceFilter,
        IReadOnlyList<string>? ComparisonDimensions,
        string? PreferredProvider) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required. Environments from different tenants cannot be compared.");
            RuleFor(x => x.SubjectEnvironmentId).NotEmpty();
            RuleFor(x => x.ReferenceEnvironmentId).NotEmpty();
            RuleFor(x => x.SubjectEnvironmentName).NotEmpty();
            RuleFor(x => x.ReferenceEnvironmentName).NotEmpty();
            RuleFor(x => x).Must(x => x.SubjectEnvironmentId != x.ReferenceEnvironmentId)
                .WithMessage("Subject and reference environments must be different.");
        }
    }

    public sealed class Handler(
        IExternalAIRoutingPort externalAiRoutingPort,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var correlationId = Guid.NewGuid().ToString();
            var comparedAt = dateTimeProvider.UtcNow;
            var groundingContext = BuildGroundingContext(request);

            const string query =
                "Compare these two environments from the same tenant and identify differences, regressions, and risks. " +
                "For each divergence, start with 'DIVERGENCE:' followed by severity (HIGH/MEDIUM/LOW), a pipe '|', dimension (e.g. contracts/telemetry/incidents/topology), a pipe '|', description. " +
                "Then provide PROMOTION_RECOMMENDATION: SAFE_TO_PROMOTE, REVIEW_REQUIRED, or BLOCK_PROMOTION. " +
                "Then SUMMARY: followed by a concise comparison summary. " +
                "Be specific and evidence-based.";

            string aiContent;
            bool isFallback;
            try
            {
                aiContent = await externalAiRoutingPort.RouteQueryAsync(groundingContext, query, request.PreferredProvider, cancellationToken: cancellationToken);
                isFallback = aiContent.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for environment comparison. TenantId={TenantId} CorrelationId={CorrelationId}", request.TenantId, correlationId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var divergences = ParseDivergences(aiContent);
            var promotionRecommendation = ParsePromotionRecommendation(aiContent);
            var summary = ParseSummary(aiContent);

            logger.LogInformation(
                "Environment comparison completed. TenantId={TenantId} Subject={Subject} Reference={Reference} Divergences={Count} Recommendation={Rec} CorrelationId={CorrelationId}",
                request.TenantId, request.SubjectEnvironmentName, request.ReferenceEnvironmentName, divergences.Count, promotionRecommendation, correlationId);

            return Result<Response>.Success(new Response(
                TenantId: request.TenantId,
                SubjectEnvironmentId: request.SubjectEnvironmentId,
                SubjectEnvironmentName: request.SubjectEnvironmentName,
                ReferenceEnvironmentId: request.ReferenceEnvironmentId,
                ReferenceEnvironmentName: request.ReferenceEnvironmentName,
                Divergences: divergences,
                PromotionRecommendation: promotionRecommendation,
                Summary: summary,
                RawAnalysis: aiContent,
                IsFallback: isFallback,
                CorrelationId: correlationId,
                ComparedAt: comparedAt));
        }

        private static string BuildGroundingContext(Command request)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"SUBJECT environment: {request.SubjectEnvironmentName} (Profile: {request.SubjectEnvironmentProfile})");
            sb.AppendLine($"REFERENCE environment: {request.ReferenceEnvironmentName} (Profile: {request.ReferenceEnvironmentProfile})");
            sb.AppendLine($"Tenant: {request.TenantId}");
            sb.AppendLine("IMPORTANT: Both environments belong to the same tenant. Comparison is always intra-tenant.");
            if (request.ServiceFilter?.Count > 0)
                sb.AppendLine($"Services in scope: {string.Join(", ", request.ServiceFilter)}");
            if (request.ComparisonDimensions?.Count > 0)
                sb.AppendLine($"Comparison dimensions: {string.Join(", ", request.ComparisonDimensions)}");
            else
                sb.AppendLine("Comparison dimensions: contracts, telemetry, incidents, topology, deployments");
            sb.AppendLine("Goal: Identify regressions, divergences and risks that may indicate the subject environment is not ready for promotion.");
            return sb.ToString();
        }

        private static List<EnvironmentDivergence> ParseDivergences(string content)
        {
            var divergences = new List<EnvironmentDivergence>();
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("DIVERGENCE:", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = trimmed["DIVERGENCE:".Length..].Split('|');
                if (parts.Length < 3) continue;
                divergences.Add(new EnvironmentDivergence(parts[0].Trim(), parts[1].Trim(), parts[2].Trim()));
            }
            return divergences;
        }

        private static string ParsePromotionRecommendation(string content)
        {
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("PROMOTION_RECOMMENDATION:", StringComparison.OrdinalIgnoreCase)) continue;
                var value = trimmed["PROMOTION_RECOMMENDATION:".Length..].Trim().ToUpperInvariant();
                if (value is "SAFE_TO_PROMOTE" or "REVIEW_REQUIRED" or "BLOCK_PROMOTION") return value;
            }
            return "REVIEW_REQUIRED";
        }

        private static string ParseSummary(string content)
        {
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                    return trimmed["SUMMARY:".Length..].Trim();
            }
            return string.Empty;
        }
    }

    public sealed record Response(
        string TenantId,
        string SubjectEnvironmentId,
        string SubjectEnvironmentName,
        string ReferenceEnvironmentId,
        string ReferenceEnvironmentName,
        IReadOnlyList<EnvironmentDivergence> Divergences,
        string PromotionRecommendation,
        string Summary,
        string RawAnalysis,
        bool IsFallback,
        string CorrelationId,
        DateTimeOffset ComparedAt);

    public sealed record EnvironmentDivergence(
        string Severity,
        string Dimension,
        string Description);
}
