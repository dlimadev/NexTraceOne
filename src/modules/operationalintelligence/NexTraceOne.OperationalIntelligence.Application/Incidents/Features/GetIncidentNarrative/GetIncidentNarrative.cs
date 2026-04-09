using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentNarrative;

/// <summary>
/// Feature: GetIncidentNarrative — consulta a narrativa gerada para um incidente.
/// Retorna todos os campos estruturados da narrativa incluindo secções individuais.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetIncidentNarrative
{
    /// <summary>Query para obter a narrativa de um incidente.</summary>
    public sealed record Query(Guid IncidentId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
        }
    }

    /// <summary>Handler que consulta a narrativa pelo incidente associado.</summary>
    public sealed class Handler(
        IIncidentNarrativeRepository narrativeRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var narrative = await narrativeRepository.GetByIncidentIdAsync(request.IncidentId, cancellationToken);
            if (narrative is null)
            {
                return IncidentErrors.NarrativeNotFound(request.IncidentId.ToString());
            }

            return new Response(
                narrative.Id.Value,
                narrative.IncidentId,
                narrative.NarrativeText,
                narrative.SymptomsSection,
                narrative.TimelineSection,
                narrative.ProbableCauseSection,
                narrative.MitigationSection,
                narrative.RelatedChangesSection,
                narrative.AffectedServicesSection,
                narrative.ModelUsed,
                narrative.TokensUsed,
                narrative.Status.ToString(),
                narrative.GeneratedAt,
                narrative.LastRefreshedAt,
                narrative.RefreshCount);
        }
    }

    /// <summary>Resposta completa da narrativa de incidente.</summary>
    public sealed record Response(
        Guid NarrativeId,
        Guid IncidentId,
        string NarrativeText,
        string? SymptomsSection,
        string? TimelineSection,
        string? ProbableCauseSection,
        string? MitigationSection,
        string? RelatedChangesSection,
        string? AffectedServicesSection,
        string ModelUsed,
        int TokensUsed,
        string Status,
        DateTimeOffset GeneratedAt,
        DateTimeOffset? LastRefreshedAt,
        int RefreshCount);
}
