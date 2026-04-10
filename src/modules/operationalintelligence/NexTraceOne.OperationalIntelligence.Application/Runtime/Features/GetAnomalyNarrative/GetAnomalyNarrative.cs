using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetAnomalyNarrative;

/// <summary>
/// Feature: GetAnomalyNarrative — consulta a narrativa gerada para uma anomalia (drift finding).
/// Retorna todos os campos estruturados da narrativa incluindo secções individuais.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetAnomalyNarrative
{
    /// <summary>Query para obter a narrativa de uma anomalia.</summary>
    public sealed record Query(Guid DriftFindingId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DriftFindingId).NotEmpty();
        }
    }

    /// <summary>Handler que consulta a narrativa pelo drift finding associado.</summary>
    public sealed class Handler(
        IAnomalyNarrativeRepository narrativeRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var driftFindingId = DriftFindingId.From(request.DriftFindingId);

            var narrative = await narrativeRepository.GetByDriftFindingIdAsync(driftFindingId, cancellationToken);
            if (narrative is null)
            {
                return RuntimeIntelligenceErrors.AnomalyNarrativeNotFound(request.DriftFindingId.ToString());
            }

            return new Response(
                narrative.Id.Value,
                narrative.DriftFindingId.Value,
                narrative.NarrativeText,
                narrative.SymptomsSection,
                narrative.BaselineComparisonSection,
                narrative.ProbableCauseSection,
                narrative.CorrelatedChangesSection,
                narrative.RecommendedActionsSection,
                narrative.SeverityJustificationSection,
                narrative.ModelUsed,
                narrative.TokensUsed,
                narrative.Status.ToString(),
                narrative.GeneratedAt,
                narrative.LastRefreshedAt,
                narrative.RefreshCount);
        }
    }

    /// <summary>Resposta completa da narrativa de anomalia.</summary>
    public sealed record Response(
        Guid NarrativeId,
        Guid DriftFindingId,
        string NarrativeText,
        string? SymptomsSection,
        string? BaselineComparisonSection,
        string? ProbableCauseSection,
        string? CorrelatedChangesSection,
        string? RecommendedActionsSection,
        string? SeverityJustificationSection,
        string ModelUsed,
        int TokensUsed,
        string Status,
        DateTimeOffset GeneratedAt,
        DateTimeOffset? LastRefreshedAt,
        int RefreshCount);
}
