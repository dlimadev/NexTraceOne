using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ListBreakGlassRequests;

/// <summary>
/// Feature: ListBreakGlassRequests — lista solicitações Break Glass do tenant atual.
/// Retorna ativas e com post-mortem pendente para visibilidade administrativa.
/// </summary>
public static class ListBreakGlassRequests
{
    /// <summary>Consulta de solicitações Break Glass.</summary>
    public sealed record Query() : IQuery<IReadOnlyList<BreakGlassResponse>>;

    /// <summary>Handler que retorna as solicitações Break Glass do tenant.</summary>
    public sealed class Handler(
        IBreakGlassRepository breakGlassRepository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, IReadOnlyList<BreakGlassResponse>>
    {
        public async Task<Result<IReadOnlyList<BreakGlassResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tenantId = TenantId.From(currentTenant.Id);

            var active = await breakGlassRepository.ListActiveByTenantAsync(tenantId, cancellationToken);
            var pendingPostMortem = await breakGlassRepository.ListPendingPostMortemAsync(tenantId, cancellationToken);

            var allRequests = active.Concat(pendingPostMortem)
                .DistinctBy(x => x.Id.Value)
                .Select(x => new BreakGlassResponse(
                    x.Id.Value,
                    x.RequestedBy.Value,
                    x.Justification,
                    x.Status.ToString(),
                    x.RequestedAt,
                    x.ExpiresAt,
                    x.RevokedAt,
                    x.PostMortemNotes is not null))
                .ToList();

            return allRequests;
        }
    }

    /// <summary>Representação de uma solicitação Break Glass para a API.</summary>
    public sealed record BreakGlassResponse(
        Guid Id,
        Guid RequestedBy,
        string Justification,
        string Status,
        DateTimeOffset RequestedAt,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? RevokedAt,
        bool HasPostMortem);
}
