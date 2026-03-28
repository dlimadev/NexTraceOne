using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetCorrelatedChanges;

/// <summary>
/// Feature: GetCorrelatedChanges — retorna as correlações dinâmicas persistidas para um incidente.
/// Utiliza o repositório de correlações (resultados do motor CorrelateIncidentWithChanges).
/// </summary>
public static class GetCorrelatedChanges
{
    /// <summary>Query para obter as correlações persistidas de um incidente.</summary>
    public sealed record Query(Guid IncidentId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna as correlações persistidas do incidente.</summary>
    public sealed class Handler(
        IIncidentStore incidentStore,
        IIncidentCorrelationRepository correlationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!incidentStore.IncidentExists(request.IncidentId.ToString()))
                return IncidentErrors.IncidentNotFound(request.IncidentId.ToString());

            var correlations = await correlationRepository.GetByIncidentIdAsync(
                request.IncidentId, cancellationToken);

            var items = correlations
                .Select(c => new CorrelatedChangeItem(
                    c.ChangeId,
                    c.ServiceId,
                    c.ServiceName,
                    c.ChangeDescription,
                    c.ChangeEnvironment,
                    c.ChangeOccurredAt,
                    c.ConfidenceLevel,
                    c.MatchType,
                    c.TimeWindowHours,
                    c.CorrelatedAt))
                .ToList();

            return Result<Response>.Success(new Response(
                request.IncidentId,
                items.Count,
                items));
        }
    }

    /// <summary>Resposta com as correlações dinâmicas persistidas do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        int TotalCorrelations,
        IReadOnlyList<CorrelatedChangeItem> Correlations);

    /// <summary>Item de correlação dinâmica persistida.</summary>
    public sealed record CorrelatedChangeItem(
        Guid ChangeId,
        Guid ServiceId,
        string ServiceName,
        string Description,
        string Environment,
        DateTimeOffset OccurredAt,
        CorrelationConfidenceLevel ConfidenceLevel,
        CorrelationMatchType MatchType,
        int TimeWindowHours,
        DateTimeOffset CorrelatedAt);
}
