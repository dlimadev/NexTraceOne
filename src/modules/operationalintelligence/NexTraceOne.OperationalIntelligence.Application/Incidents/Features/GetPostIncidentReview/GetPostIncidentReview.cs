using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetPostIncidentReview;

/// <summary>
/// Feature: GetPostIncidentReview — consulta o PIR associado a um incidente.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetPostIncidentReview
{
    /// <summary>Query para obter o PIR de um incidente.</summary>
    public sealed record Query(Guid IncidentId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
        }
    }

    /// <summary>Handler que consulta o PIR pelo incidente associado.</summary>
    public sealed class Handler(
        IPostIncidentReviewRepository reviewRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var review = await reviewRepository.GetByIncidentIdAsync(request.IncidentId, cancellationToken);
            if (review is null)
            {
                return IncidentErrors.PirNotFound(request.IncidentId.ToString());
            }

            return new Response(
                review.Id.Value,
                review.IncidentId,
                review.CurrentPhase.ToString(),
                review.Outcome.ToString(),
                review.IsCompleted,
                review.ResponsibleTeam,
                review.Facilitator,
                review.RootCauseAnalysis,
                review.PreventiveActionsJson,
                review.TimelineNarrative,
                review.Summary,
                review.StartedAt,
                review.CompletedAt);
        }
    }

    /// <summary>Resposta completa do PIR.</summary>
    public sealed record Response(
        Guid ReviewId,
        Guid IncidentId,
        string CurrentPhase,
        string Outcome,
        bool IsCompleted,
        string ResponsibleTeam,
        string? Facilitator,
        string? RootCauseAnalysis,
        string? PreventiveActionsJson,
        string? TimelineNarrative,
        string? Summary,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);
}
