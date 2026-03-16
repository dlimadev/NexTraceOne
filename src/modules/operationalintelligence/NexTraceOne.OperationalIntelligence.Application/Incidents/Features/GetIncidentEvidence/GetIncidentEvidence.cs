using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
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
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var evidence = store.GetIncidentEvidence(request.IncidentId);
            if (evidence is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(evidence));
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
