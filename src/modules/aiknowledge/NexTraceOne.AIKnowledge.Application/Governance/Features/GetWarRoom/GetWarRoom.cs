using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetWarRoom;

/// <summary>
/// Feature: GetWarRoom — obtém os detalhes de uma War Room pelo identificador.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetWarRoom
{
    public sealed record Query(Guid WarRoomSessionId) : IQuery<Response>;

    public sealed class Handler(IAiWarRoomRepository warRoomRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var session = await warRoomRepository.GetByIdAsync(
                WarRoomSessionId.From(request.WarRoomSessionId), ct);

            if (session is null)
                return AiGovernanceErrors.WarRoomNotFound(request.WarRoomSessionId.ToString());

            return new Response(
                session.Id.Value,
                session.IncidentId,
                session.IncidentTitle,
                session.Severity,
                session.Status,
                session.ServiceAffected,
                session.ParticipantUserIds,
                session.TimelineJson,
                session.SuggestedActionsJson,
                session.PostMortemDraft,
                session.OpenedAt,
                session.ResolvedAt,
                session.SkillUsed);
        }
    }

    public sealed record Response(
        Guid WarRoomSessionId,
        string IncidentId,
        string IncidentTitle,
        string Severity,
        string Status,
        string ServiceAffected,
        IReadOnlyList<string> ParticipantUserIds,
        string TimelineJson,
        string SuggestedActionsJson,
        string PostMortemDraft,
        DateTimeOffset OpenedAt,
        DateTimeOffset? ResolvedAt,
        string SkillUsed);
}
