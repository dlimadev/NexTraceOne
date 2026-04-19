using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListWarRooms;

/// <summary>
/// Feature: ListWarRooms — lista War Rooms de um tenant com filtro opcional por status.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListWarRooms
{
    public sealed record Query(Guid TenantId, string? StatusFilter) : IQuery<Response>;

    public sealed class Handler(IAiWarRoomRepository warRoomRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            IReadOnlyList<WarRoomSession> sessions;

            if (string.Equals(request.StatusFilter, "open", StringComparison.OrdinalIgnoreCase))
                sessions = await warRoomRepository.ListOpenAsync(request.TenantId, ct);
            else
                sessions = await warRoomRepository.ListByTenantAsync(request.TenantId, ct);

            var items = sessions.Select(s => new WarRoomSummary(
                s.Id.Value, s.IncidentId, s.IncidentTitle, s.Severity, s.Status, s.ServiceAffected, s.OpenedAt
            )).ToList().AsReadOnly();

            return new Response(items, items.Count);
        }
    }

    public sealed record WarRoomSummary(
        Guid WarRoomSessionId,
        string IncidentId,
        string IncidentTitle,
        string Severity,
        string Status,
        string ServiceAffected,
        DateTimeOffset OpenedAt);

    public sealed record Response(IReadOnlyList<WarRoomSummary> Items, int TotalCount);
}
