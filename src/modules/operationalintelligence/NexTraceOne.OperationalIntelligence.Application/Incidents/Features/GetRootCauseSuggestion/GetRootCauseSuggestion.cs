using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRootCauseSuggestion;

/// <summary>
/// Feature: GetRootCauseSuggestion — analisa a timeline de mudanças + correlação de incidente
/// para sugerir a causa mais provável e os passos de investigação recomendados.
///
/// Algoritmo:
///   1. Analisa correlações de mudança (tipo, confiança, distância temporal)
///   2. Identifica a mudança mais suspeita (mais recente + maior confiança)
///   3. Classifica a causa sugerida por categoria (Deployment, Configuration, Infrastructure, Unknown)
///   4. Deriva investigação steps baseados no tipo de causa
///
/// Valor: reduz o tempo médio de identificação de causa raiz de horas para minutos.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetRootCauseSuggestion
{
    /// <summary>Query para obter a sugestão de causa raiz de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que analisa correlações e deriva a sugestão de causa raiz.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var correlation = store.GetIncidentCorrelation(request.IncidentId);
            var evidence = store.GetIncidentEvidence(request.IncidentId);
            var detail = store.GetIncidentDetail(request.IncidentId);

            if (detail is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            if (correlation is null || correlation.RelatedChanges?.Count == 0)
            {
                return Task.FromResult(Result<Response>.Success(new Response(
                    IncidentId: request.IncidentId,
                    HasSuggestion: false,
                    SuggestedCauseCategory: "Unknown",
                    SuggestedCauseSummary: "No correlated changes found. Manual investigation required.",
                    ConfidenceLevel: "Insufficient",
                    PrimaryChangeId: null,
                    PrimaryChangeDescription: null,
                    InvestigationSteps: new[]
                    {
                        "Review recent deployments in the affected environment.",
                        "Check infrastructure and configuration changes in the last 24 hours.",
                        "Analyse error logs and traces for the affected service.",
                        "Check for upstream dependency degradation."
                    },
                    SupportingEvidence: Array.Empty<string>())));
            }

            var primaryChange = correlation.RelatedChanges
                .OrderByDescending(c => c.DeployedAt)
                .First();

            var (category, summary) = DetermineCategory(
                primaryChange.Description, primaryChange.ChangeType,
                primaryChange.DeployedAt, detail.ImpactedEnvironment);
            var steps = DeriveInvestigationSteps(category, primaryChange.Description);
            var evidence_signals = DeriveEvidenceSignals(
                evidence?.DegradationSummary, evidence?.OperationalSignalsSummary,
                correlation.RelatedChanges.Count, correlation.Confidence.ToString());

            var confidenceLevel = correlation.Confidence switch
            {
                CorrelationConfidence.Confirmed => "High",
                CorrelationConfidence.High => "High",
                CorrelationConfidence.Medium => "Medium",
                _ => "Low"
            };

            return Task.FromResult(Result<Response>.Success(new Response(
                IncidentId: request.IncidentId,
                HasSuggestion: true,
                SuggestedCauseCategory: category,
                SuggestedCauseSummary: summary,
                ConfidenceLevel: confidenceLevel,
                PrimaryChangeId: primaryChange.ChangeId,
                PrimaryChangeDescription: primaryChange.Description,
                InvestigationSteps: steps,
                SupportingEvidence: evidence_signals)));
        }

        private static (string Category, string Summary) DetermineCategory(
            string changeDescription,
            string? changeType,
            DateTimeOffset deployedAt,
            string? impactedEnvironment)
        {
            var changeTypeUpper = changeType?.ToUpperInvariant() ?? string.Empty;

            if (changeTypeUpper.Contains("DEPLOY") || changeTypeUpper.Contains("RELEASE"))
            {
                return ("Deployment",
                    $"Recent deployment of '{changeDescription}' deployed at {deployedAt:u} " +
                    $"is the primary suspect. Deployment to '{impactedEnvironment}' coincides with incident onset.");
            }

            if (changeTypeUpper.Contains("CONFIG") || changeTypeUpper.Contains("CONFIGURATION"))
            {
                return ("Configuration",
                    $"Configuration change '{changeDescription}' may have introduced unexpected behaviour.");
            }

            if (changeTypeUpper.Contains("INFRA") || changeTypeUpper.Contains("INFRASTRUCTURE"))
            {
                return ("Infrastructure",
                    $"Infrastructure change '{changeDescription}' may have caused service degradation.");
            }

            return ("Deployment",
                $"Change '{changeDescription}' deployed at {deployedAt:u} " +
                $"is correlated with incident onset.");
        }

        private static IReadOnlyList<string> DeriveInvestigationSteps(string category, string changeDescription)
            => category switch
            {
                "Deployment" =>
                [
                    $"1. Inspect the deployment '{changeDescription}' — diff changes vs previous version.",
                    "2. Check error rate spike in APM traces immediately after deployment time.",
                    "3. Verify if rollback of this deployment resolves the incident.",
                    "4. Review deployment logs for errors or warnings during startup.",
                    "5. Check dependent services for cascading failures."
                ],
                "Configuration" =>
                [
                    $"1. Review the configuration change '{changeDescription}' for unexpected values.",
                    "2. Compare running configuration with last known good state.",
                    "3. Check if rolling back the configuration change resolves the incident.",
                    "4. Verify that all dependent services were notified of the config change."
                ],
                "Infrastructure" =>
                [
                    $"1. Review infrastructure change '{changeDescription}' — resource limits, network rules.",
                    "2. Check platform health metrics (CPU, memory, disk, network) post-change.",
                    "3. Verify cluster/node health in affected environment.",
                    "4. Review infrastructure change logs for errors."
                ],
                _ =>
                [
                    "1. Review all changes in the affected environment in the last 4 hours.",
                    "2. Analyse error logs and traces for the affected service.",
                    "3. Check upstream dependencies for degradation.",
                    "4. Escalate to owning team with full context."
                ]
            };

        private static IReadOnlyList<string> DeriveEvidenceSignals(
            string? degradationSummary,
            string? operationalSignalsSummary,
            int correlatedChangesCount,
            string correlationConfidence)
        {
            var signals = new List<string>();

            if (!string.IsNullOrWhiteSpace(degradationSummary))
                signals.Add($"Degradation: {degradationSummary}");

            if (!string.IsNullOrWhiteSpace(operationalSignalsSummary))
                signals.Add($"Signals: {operationalSignalsSummary}");

            signals.Add($"Correlated changes: {correlatedChangesCount}");
            signals.Add($"Correlation confidence: {correlationConfidence}");

            return signals.AsReadOnly();
        }
    }

    /// <summary>Resposta da sugestão de causa raiz de incidente.</summary>
    public sealed record Response(
        string IncidentId,
        bool HasSuggestion,
        string SuggestedCauseCategory,
        string SuggestedCauseSummary,
        string ConfidenceLevel,
        Guid? PrimaryChangeId,
        string? PrimaryChangeDescription,
        IReadOnlyList<string> InvestigationSteps,
        IReadOnlyList<string> SupportingEvidence);
}
