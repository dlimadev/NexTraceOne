using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeDecisionHistory;

/// <summary>
/// Feature: GetChangeDecisionHistory — devolve o histórico de decisões de governança de uma release.
/// Retorna todos os eventos associados, incluindo aprovações, rejeições e condições.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetChangeDecisionHistory
{
    /// <summary>Query para obter o histórico de decisões de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de histórico de decisões.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que carrega todos os eventos da release e os mapeia para DTOs de histórico.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeEventRepository changeEventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var events = await changeEventRepository.ListByReleaseIdAsync(releaseId, cancellationToken);

            var decisions = events
                .Select(e => new DecisionHistoryItemDto(
                    e.Id.Value,
                    e.EventType,
                    e.Description,
                    e.Source,
                    e.OccurredAt))
                .ToList();

            return new Response(release.Id.Value, decisions);
        }
    }

    /// <summary>DTO de item individual do histórico de decisões.</summary>
    public sealed record DecisionHistoryItemDto(
        Guid EventId,
        string EventType,
        string Description,
        string Source,
        DateTimeOffset OccurredAt);

    /// <summary>Resposta com o histórico completo de decisões de governança da release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        IReadOnlyList<DecisionHistoryItemDto> Decisions);
}
