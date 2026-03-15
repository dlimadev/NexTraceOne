using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;

/// <summary>
/// Feature: GetIncidentEvidence — retorna as evidências operacionais de um incidente.
/// Inclui sinais operacionais, indicadores de degradação, observações e notas.
/// Base para futura IA operacional e análise assistida.
/// </summary>
public static class GetIncidentEvidence
{
    /// <summary>Query para obter evidências de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe as evidências do incidente.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var evidence = FindEvidence(request.IncidentId);
            if (evidence is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(evidence));
        }

        private static Response? FindEvidence(string incidentId)
        {
            if (incidentId.Equals("a1b2c3d4-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    OperationalSignalsSummary: "Error rate: 1.2% → 8.2%. P99 latency: 120ms → 890ms. Timeout rate increased 5x.",
                    DegradationSummary: "Payment success rate dropped from 98.8% to 91.8%. SLO breach confirmed.",
                    Observations: new[]
                    {
                        new EvidenceObservation("Error rate spike", "Error rate increased from 1.2% to 8.2% within 15 minutes of deployment"),
                        new EvidenceObservation("Latency degradation", "P99 latency increased from 120ms to 890ms"),
                        new EvidenceObservation("Downstream impact", "Order API reporting payment timeouts"),
                        new EvidenceObservation("Temporal correlation", "Metrics degradation started exactly at deployment time of v2.14.0"),
                    },
                    AnomalySummary: "Clear before/after pattern: all key metrics degraded immediately post-deployment.",
                    Notes: "Deployment window: 09:00–09:15 UTC. Error rate first exceeded threshold at 09:17 UTC.");
            }

            if (incidentId.Equals("a1b2c3d4-0002-0000-0000-000000000002", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    OperationalSignalsSummary: "External API returning 503 since 06:00 UTC. Retry queue depth: 1,247.",
                    DegradationSummary: "Product catalog sync halted. Stale data risk for product listings.",
                    Observations: new[]
                    {
                        new EvidenceObservation("External API failure", "503 Service Unavailable from catalog-provider.example.com"),
                        new EvidenceObservation("Queue buildup", "Sync retry queue depth at 1,247 messages"),
                    },
                    AnomalySummary: "External dependency failure — no internal anomalies detected.",
                    Notes: null);
            }

            return null;
        }
    }

    /// <summary>Resposta de evidências do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        string OperationalSignalsSummary,
        string DegradationSummary,
        IReadOnlyList<EvidenceObservation> Observations,
        string AnomalySummary,
        string? Notes);

    /// <summary>Observação de evidência individual.</summary>
    public sealed record EvidenceObservation(string Title, string Description);
}
