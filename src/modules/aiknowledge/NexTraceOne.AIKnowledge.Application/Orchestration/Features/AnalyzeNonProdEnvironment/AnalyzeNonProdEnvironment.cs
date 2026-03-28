using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.AnalyzeNonProdEnvironment;

/// <summary>
/// Feature: AnalyzeNonProdEnvironment — A IA analisa um ambiente não produtivo e identifica sinais
/// que podem representar risco para produção.
///
/// Este é o caso de uso mais estratégico do produto: detectar regressões, anomalias e
/// problemas de qualidade ANTES que avancem para produção.
///
/// O contexto é passado explicitamente (não inferido) — o backend é a fonte de verdade.
/// </summary>
public static class AnalyzeNonProdEnvironment
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    public sealed record Command(
        /// <summary>Identificador do tenant (obrigatório — isolamento garantido).</summary>
        string TenantId,
        /// <summary>Identificador do ambiente não produtivo a ser analisado.</summary>
        string EnvironmentId,
        /// <summary>Nome do ambiente para contexto humano e grounding.</summary>
        string EnvironmentName,
        /// <summary>Perfil do ambiente (ex.: qa, staging, uat, development).</summary>
        string EnvironmentProfile,
        /// <summary>Serviços a incluir na análise. Null = todos os serviços acessíveis.</summary>
        IReadOnlyList<string>? ServiceFilter,
        /// <summary>Janela de análise em dias (padrão: 7).</summary>
        int ObservationWindowDays,
        /// <summary>Provider de IA preferido (null = routing automático).</summary>
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        /// <summary>
        /// Perfis de ambiente que representam produção ou equivalente de produção.
        /// Análise não-produtiva é proibida nestes perfis por política de segurança.
        /// </summary>
        private static readonly HashSet<string> ProductionLikeProfiles =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "production",
                "disasterrecovery",
                "dr",
            };

        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required for environment analysis.");
            RuleFor(x => x.EnvironmentId).NotEmpty().WithMessage("EnvironmentId is required for non-prod analysis.");
            RuleFor(x => x.EnvironmentName).NotEmpty();
            RuleFor(x => x.EnvironmentProfile)
                .NotEmpty()
                .Must(profile => !ProductionLikeProfiles.Contains(profile))
                .WithMessage(
                    "Non-prod analysis cannot be executed against a production or disaster-recovery environment. " +
                    "Provide a non-production environment profile (e.g. qa, staging, uat, development, sandbox).");
            RuleFor(x => x.ObservationWindowDays).InclusiveBetween(1, 90);
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
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.EnvironmentId);

            var correlationId = Guid.NewGuid().ToString();
            var analysisStarted = dateTimeProvider.UtcNow;

            var groundingContext = BuildGroundingContext(request);
            const string query =
                "Analyze this non-production environment and identify signals that could represent risk to production. " +
                "For each finding, start a new line with 'FINDING:' followed by severity (HIGH/MEDIUM/LOW), a pipe '|', category, a pipe '|', and description. " +
                "Then provide an OVERALL_RISK: HIGH, MEDIUM, or LOW. " +
                "Then provide RECOMMENDATION: followed by your recommendation. " +
                "Be specific, evidence-based, and concise.";

            string aiContent;
            bool isFallback;
            try
            {
                aiContent = await externalAiRoutingPort.RouteQueryAsync(
                    groundingContext,
                    query,
                    request.PreferredProvider,
                    cancellationToken: cancellationToken);
                isFallback = aiContent.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "AI provider unavailable for non-prod analysis. TenantId={TenantId} EnvironmentId={EnvironmentId} CorrelationId={CorrelationId}",
                    request.TenantId, request.EnvironmentId, correlationId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var findings = ParseFindings(aiContent);
            var overallRisk = ParseOverallRisk(aiContent);
            var recommendation = ParseRecommendation(aiContent);

            logger.LogInformation(
                "Non-prod analysis completed. TenantId={TenantId} EnvironmentId={EnvironmentId} Findings={FindingCount} Risk={Risk} CorrelationId={CorrelationId}",
                request.TenantId, request.EnvironmentId, findings.Count, overallRisk, correlationId);

            return Result<Response>.Success(new Response(
                TenantId: request.TenantId,
                EnvironmentId: request.EnvironmentId,
                EnvironmentName: request.EnvironmentName,
                EnvironmentProfile: request.EnvironmentProfile,
                Findings: findings,
                OverallRiskLevel: overallRisk,
                Recommendation: recommendation,
                RawAnalysis: aiContent,
                IsFallback: isFallback,
                CorrelationId: correlationId,
                AnalyzedAt: analysisStarted,
                ObservationWindowDays: request.ObservationWindowDays));
        }

        private static string BuildGroundingContext(Command request)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Environment: {request.EnvironmentName} (Profile: {request.EnvironmentProfile})");
            sb.AppendLine($"Tenant: {request.TenantId}");
            sb.AppendLine($"Analysis window: last {request.ObservationWindowDays} days");
            sb.AppendLine("Analysis type: Non-production environment risk assessment for production promotion prevention");
            if (request.ServiceFilter?.Count > 0)
                sb.AppendLine($"Services in scope: {string.Join(", ", request.ServiceFilter)}");
            else
                sb.AppendLine("Services in scope: all accessible services in this environment");
            sb.AppendLine("Goal: Identify signals that could represent regression, quality issues, or risk to production.");
            return sb.ToString();
        }

        private static List<AnalysisFinding> ParseFindings(string content)
        {
            var findings = new List<AnalysisFinding>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("FINDING:", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = trimmed["FINDING:".Length..].Split('|');
                if (parts.Length < 3) continue;
                findings.Add(new AnalysisFinding(
                    Severity: parts[0].Trim(),
                    Category: parts[1].Trim(),
                    Description: parts[2].Trim()));
            }
            return findings;
        }

        private static string ParseOverallRisk(string content)
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("OVERALL_RISK:", StringComparison.OrdinalIgnoreCase)) continue;
                var value = trimmed["OVERALL_RISK:".Length..].Trim().ToUpperInvariant();
                if (value is "HIGH" or "MEDIUM" or "LOW") return value;
            }
            return "UNKNOWN";
        }

        private static string ParseRecommendation(string content)
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("RECOMMENDATION:", StringComparison.OrdinalIgnoreCase))
                    return trimmed["RECOMMENDATION:".Length..].Trim();
            }
            return string.Empty;
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resultado da análise de ambiente não produtivo.</summary>
    public sealed record Response(
        string TenantId,
        string EnvironmentId,
        string EnvironmentName,
        string EnvironmentProfile,
        IReadOnlyList<AnalysisFinding> Findings,
        string OverallRiskLevel,
        string Recommendation,
        string RawAnalysis,
        bool IsFallback,
        string CorrelationId,
        DateTimeOffset AnalyzedAt,
        int ObservationWindowDays);

    /// <summary>Achado individual da análise.</summary>
    public sealed record AnalysisFinding(
        string Severity,
        string Category,
        string Description);
}
