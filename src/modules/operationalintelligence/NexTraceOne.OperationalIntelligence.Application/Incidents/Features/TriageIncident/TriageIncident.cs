using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.TriageIncident;

/// <summary>
/// Feature: TriageIncident — classifica automaticamente a severidade de um incidente
/// com base no padrão de correlação, serviços afetados, tipo de incidente e histórico.
///
/// Lógica de auto-triage:
///   - Incidente com correlação alta + serviço crítico + Breaking change → Critical
///   - Incidente com correlação moderada + degradação reportada → High
///   - Incidente recente sem correlação clara → Medium
///   - Incidente informativo ou sem degradação → Low
///
/// O triage é uma sugestão — a equipa pode sobrepor manualmente a severidade.
/// Integra com Change Intelligence para usar a correlação de mudanças.
///
/// Valor: reduz tempo de triagem de incidentes de minutos para segundos.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class TriageIncident
{
    /// <summary>Query para obter a sugestão de triage automático de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que executa a lógica de auto-triage baseada em correlação e sinais.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var detail = store.GetIncidentDetail(request.IncidentId);
            if (detail is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var correlation = store.GetIncidentCorrelation(request.IncidentId);
            var mitigation = store.GetMitigationRecommendations(request.IncidentId);

            var relatedChangesCount = correlation?.RelatedChanges?.Count ?? 0;
            var correlationConfidence = correlation?.Confidence;

            var (suggestedSeverity, confidence, rationale, signals) = DetermineTriageSeverity(
                detail.Identity.IncidentType, detail.ImpactedEnvironment, relatedChangesCount, correlationConfidence);

            return Task.FromResult(Result<Response>.Success(new Response(
                IncidentId: request.IncidentId,
                CurrentSeverity: detail.Identity.Severity,
                SuggestedSeverity: suggestedSeverity,
                TriageConfidence: confidence,
                Rationale: rationale,
                TriageSignals: signals,
                HasActiveCorrelation: relatedChangesCount > 0,
                RecommendedAction: DetermineRecommendedAction(suggestedSeverity, relatedChangesCount))));
        }

        private static (IncidentSeverity Severity, string Confidence, string Rationale, IReadOnlyList<string> Signals)
            DetermineTriageSeverity(
                IncidentType incidentType,
                string? impactedEnvironment,
                int relatedChangesCount,
                CorrelationConfidence? correlationConfidence)
        {
            var signals = new List<string>();
            var score = 0;

            var hasHighConfidenceCorrelation = correlationConfidence
                is CorrelationConfidence.High or CorrelationConfidence.Confirmed;
            var hasModerateCorrelation = correlationConfidence
                is CorrelationConfidence.Medium;

            if (hasHighConfidenceCorrelation)
            {
                score += 3;
                signals.Add($"High confidence correlation with {relatedChangesCount} change(s) detected.");
            }
            else if (hasModerateCorrelation)
            {
                score += 1;
                signals.Add($"Moderate correlation with {relatedChangesCount} change(s) detected.");
            }
            else
            {
                signals.Add("No strong correlation with recent changes.");
            }

            if (incidentType is IncidentType.ServiceDegradation or IncidentType.AvailabilityIssue)
            {
                score += 2;
                signals.Add($"Incident type '{incidentType}' indicates active service impact.");
            }

            if (impactedEnvironment?.Equals("production", StringComparison.OrdinalIgnoreCase) == true)
            {
                score += 2;
                signals.Add("Incident occurred in production environment.");
            }

            if (relatedChangesCount >= 3)
            {
                score += 1;
                signals.Add($"Large blast radius — {relatedChangesCount} correlated changes.");
            }

            IncidentSeverity severity;
            string confidence;
            string rationale;

            if (score >= 6)
            {
                severity = IncidentSeverity.Critical;
                confidence = "High";
                rationale = "Multiple high-impact signals: high-confidence correlation, production outage, large blast radius.";
            }
            else if (score >= 4)
            {
                severity = IncidentSeverity.Major;
                confidence = "Medium";
                rationale = "Significant impact signals detected. Service degradation with correlated changes.";
            }
            else if (score >= 2)
            {
                severity = IncidentSeverity.Minor;
                confidence = "Medium";
                rationale = "Moderate signals. Correlation present but confidence is limited.";
            }
            else
            {
                severity = IncidentSeverity.Warning;
                confidence = "Low";
                rationale = "Insufficient signals for high-severity classification. Monitor for escalation.";
            }

            return (severity, confidence, rationale, signals.AsReadOnly());
        }

        private static string DetermineRecommendedAction(
            IncidentSeverity severity,
            int relatedChangesCount)
        {
            var hasRollbackCandidate = relatedChangesCount > 0;

            return severity switch
            {
                IncidentSeverity.Critical => hasRollbackCandidate
                    ? "Immediately engage on-call. Evaluate rollback of correlated change(s)."
                    : "Immediately engage on-call. Activate P1 war-room protocol.",
                IncidentSeverity.Major => hasRollbackCandidate
                    ? "Notify owning team. Investigate correlated change(s) as primary suspect."
                    : "Notify owning team. Gather telemetry for root cause analysis.",
                IncidentSeverity.Minor =>
                    "Assign to owning team. Monitor for escalation. Correlate with recent changes.",
                _ => "Monitor. No immediate action required."
            };
        }
    }

    /// <summary>Resposta do auto-triage de incidente.</summary>
    public sealed record Response(
        string IncidentId,
        IncidentSeverity CurrentSeverity,
        IncidentSeverity SuggestedSeverity,
        string TriageConfidence,
        string Rationale,
        IReadOnlyList<string> TriageSignals,
        bool HasActiveCorrelation,
        string RecommendedAction);
}
