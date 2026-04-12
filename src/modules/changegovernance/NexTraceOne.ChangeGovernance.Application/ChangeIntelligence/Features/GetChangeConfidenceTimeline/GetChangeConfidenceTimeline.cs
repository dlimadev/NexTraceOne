using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeConfidenceTimeline;

/// <summary>
/// Feature: GetChangeConfidenceTimeline — devolve a timeline completa de eventos
/// de confiança de uma release, ordenada cronologicamente.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetChangeConfidenceTimeline
{
    /// <summary>Query para obter a timeline de confiança de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de timeline de confiança.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que carrega todos os eventos de confiança da release,
    /// ordenados cronologicamente, e os mapeia para DTOs de resposta.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeConfidenceEventRepository confidenceEventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var events = await confidenceEventRepository.ListByReleaseAsync(releaseId, cancellationToken);

            var items = events
                .Select(e => new ConfidenceTimelineItemDto(
                    e.Id.Value,
                    e.EventType,
                    e.ConfidenceBefore,
                    e.ConfidenceAfter,
                    e.Reason,
                    e.Details,
                    e.Source,
                    e.OccurredAt))
                .ToList();

            var currentScore = events.Count > 0 ? events[^1].ConfidenceAfter : (int?)null;

            return new Response(release.Id.Value, currentScore, items);
        }
    }

    /// <summary>DTO de item individual da timeline de confiança.</summary>
    public sealed record ConfidenceTimelineItemDto(
        Guid EventId,
        ConfidenceEventType EventType,
        int ConfidenceBefore,
        int ConfidenceAfter,
        string Reason,
        string? Details,
        string Source,
        DateTimeOffset OccurredAt);

    /// <summary>Resposta com a timeline completa de confiança da release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        int? CurrentConfidence,
        IReadOnlyList<ConfidenceTimelineItemDto> Events);
}
