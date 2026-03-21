using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.AssessPromotionReadiness;

/// <summary>
/// Feature: AssessPromotionReadiness — A IA sintetiza um assessment de readiness para promoção
/// de um serviço de um ambiente não produtivo para produção.
///
/// Este caso de uso é central na missão de prevenção: antes de promover, consultar a IA
/// para obter um assessment estruturado com score, achados e recomendação de bloqueio.
/// </summary>
public static class AssessPromotionReadiness
{
    public sealed record Command(
        string TenantId,
        string SourceEnvironmentId,
        string SourceEnvironmentName,
        /// <summary>
        /// Indica se o ambiente de origem é equivalente a produção.
        /// Deve ser false — a promoção parte de um ambiente não produtivo.
        /// </summary>
        bool SourceIsProductionLike,
        string TargetEnvironmentId,
        string TargetEnvironmentName,
        /// <summary>
        /// Indica se o ambiente de destino é equivalente a produção.
        /// Deve ser true — a promoção avança para produção ou equivalente.
        /// </summary>
        bool TargetIsProductionLike,
        string ServiceName,
        string Version,
        string? ReleaseId,
        int ObservationWindowDays,
        string? PreferredProvider) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required for promotion readiness assessment.");
            RuleFor(x => x.SourceEnvironmentId).NotEmpty();
            RuleFor(x => x.TargetEnvironmentId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty();
            RuleFor(x => x.Version).NotEmpty();
            RuleFor(x => x.ObservationWindowDays).InclusiveBetween(1, 90);
            RuleFor(x => x).Must(x => x.SourceEnvironmentId != x.TargetEnvironmentId)
                .WithMessage("Source and target environments must be different for promotion readiness.");
            RuleFor(x => x.SourceIsProductionLike)
                .Equal(false)
                .WithMessage(
                    "Promotion readiness assessment requires a non-production source environment. " +
                    "The source environment must not be production-like (SourceIsProductionLike must be false).");
            RuleFor(x => x.TargetIsProductionLike)
                .Equal(true)
                .WithMessage(
                    "Promotion readiness assessment requires a production-like target environment. " +
                    "The target environment must be production-like (TargetIsProductionLike must be true).");
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
            Guard.Against.NullOrWhiteSpace(request.ServiceName);

            var correlationId = Guid.NewGuid().ToString();
            var assessedAt = dateTimeProvider.UtcNow;
            var groundingContext = BuildGroundingContext(request);

            const string query =
                "Assess promotion readiness for this service and provide a structured evaluation. " +
                "Provide READINESS_SCORE: a number from 0 to 100. " +
                "Provide READINESS_LEVEL: NOT_READY, NEEDS_REVIEW, or READY. " +
                "For each blocking issue, start with 'BLOCKER:' followed by category, a pipe '|', description. " +
                "For each warning, start with 'WARNING:' followed by category, a pipe '|', description. " +
                "Provide SHOULD_BLOCK: YES or NO. " +
                "Provide SUMMARY: followed by a concise readiness summary. " +
                "Be specific and action-oriented.";

            string aiContent;
            bool isFallback;
            try
            {
                aiContent = await externalAiRoutingPort.RouteQueryAsync(groundingContext, query, request.PreferredProvider, cancellationToken);
                isFallback = aiContent.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for promotion readiness. TenantId={TenantId} ServiceName={Service} CorrelationId={CorrelationId}",
                    request.TenantId, request.ServiceName, correlationId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var score = ParseReadinessScore(aiContent);
            var level = ParseReadinessLevel(aiContent);
            var blockers = ParseBlockers(aiContent);
            var warnings = ParseWarnings(aiContent);
            var shouldBlock = ParseShouldBlock(aiContent);
            var summary = ParseSummary(aiContent);

            logger.LogInformation(
                "Promotion readiness assessed. TenantId={TenantId} Service={Service} Score={Score} Level={Level} ShouldBlock={Block} CorrelationId={CorrelationId}",
                request.TenantId, request.ServiceName, score, level, shouldBlock, correlationId);

            return Result<Response>.Success(new Response(
                TenantId: request.TenantId,
                SourceEnvironmentId: request.SourceEnvironmentId,
                SourceEnvironmentName: request.SourceEnvironmentName,
                TargetEnvironmentId: request.TargetEnvironmentId,
                TargetEnvironmentName: request.TargetEnvironmentName,
                ServiceName: request.ServiceName,
                Version: request.Version,
                ReleaseId: request.ReleaseId,
                ReadinessScore: score,
                ReadinessLevel: level,
                Blockers: blockers,
                Warnings: warnings,
                ShouldBlock: shouldBlock,
                Summary: summary,
                RawAnalysis: aiContent,
                IsFallback: isFallback,
                CorrelationId: correlationId,
                AssessedAt: assessedAt));
        }

        private static string BuildGroundingContext(Command request)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Service: {request.ServiceName} v{request.Version}");
            sb.AppendLine($"Promoting FROM: {request.SourceEnvironmentName} (production-like: {request.SourceIsProductionLike}) TO: {request.TargetEnvironmentName} (production-like: {request.TargetIsProductionLike})");
            sb.AppendLine($"Tenant: {request.TenantId}");
            sb.AppendLine($"Observation window: last {request.ObservationWindowDays} days");
            if (!string.IsNullOrWhiteSpace(request.ReleaseId))
                sb.AppendLine($"Associated Release ID: {request.ReleaseId}");
            sb.AppendLine("Goal: Synthesize readiness for production promotion. Identify blockers, risks and provide actionable recommendation.");
            return sb.ToString();
        }

        private static int ParseReadinessScore(string content)
        {
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("READINESS_SCORE:", StringComparison.OrdinalIgnoreCase)) continue;
                var val = trimmed["READINESS_SCORE:".Length..].Trim();
                if (int.TryParse(val, out var score)) return Math.Clamp(score, 0, 100);
            }
            return 50;
        }

        private static string ParseReadinessLevel(string content)
        {
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("READINESS_LEVEL:", StringComparison.OrdinalIgnoreCase)) continue;
                var val = trimmed["READINESS_LEVEL:".Length..].Trim().ToUpperInvariant();
                if (val is "NOT_READY" or "NEEDS_REVIEW" or "READY") return val;
            }
            return "NEEDS_REVIEW";
        }

        private static List<ReadinessIssue> ParseBlockers(string content)
        {
            var blockers = new List<ReadinessIssue>();
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("BLOCKER:", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = trimmed["BLOCKER:".Length..].Split('|');
                if (parts.Length < 2) continue;
                blockers.Add(new ReadinessIssue("BLOCKER", parts[0].Trim(), parts[1].Trim()));
            }
            return blockers;
        }

        private static List<ReadinessIssue> ParseWarnings(string content)
        {
            var warnings = new List<ReadinessIssue>();
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = trimmed["WARNING:".Length..].Split('|');
                if (parts.Length < 2) continue;
                warnings.Add(new ReadinessIssue("WARNING", parts[0].Trim(), parts[1].Trim()));
            }
            return warnings;
        }

        private static bool ParseShouldBlock(string content)
        {
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("SHOULD_BLOCK:", StringComparison.OrdinalIgnoreCase)) continue;
                return trimmed["SHOULD_BLOCK:".Length..].Trim().Equals("YES", StringComparison.OrdinalIgnoreCase);
            }
            return false;
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
        string SourceEnvironmentId,
        string SourceEnvironmentName,
        string TargetEnvironmentId,
        string TargetEnvironmentName,
        string ServiceName,
        string Version,
        string? ReleaseId,
        int ReadinessScore,
        string ReadinessLevel,
        IReadOnlyList<ReadinessIssue> Blockers,
        IReadOnlyList<ReadinessIssue> Warnings,
        bool ShouldBlock,
        string Summary,
        string RawAnalysis,
        bool IsFallback,
        string CorrelationId,
        DateTimeOffset AssessedAt);

    public sealed record ReadinessIssue(
        string Type,
        string Category,
        string Description);
}
