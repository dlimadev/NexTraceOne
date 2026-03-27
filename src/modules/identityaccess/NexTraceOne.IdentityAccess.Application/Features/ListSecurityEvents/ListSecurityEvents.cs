using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ListSecurityEvents;

/// <summary>
/// Feature: ListSecurityEvents — lista eventos críticos de segurança do tenant atual.
/// </summary>
public static class ListSecurityEvents
{
    /// <summary>Consulta paginada de eventos de segurança com filtro opcional por tipo.</summary>
    public sealed record Query(
        string? EventType,
        int Page = 1,
        int PageSize = 50) : IQuery<Response>;

    /// <summary>Handler que retorna os eventos críticos e contagem de pendentes de revisão.</summary>
    public sealed class Handler(
        ISecurityEventRepository securityEventRepository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tenantId = TenantId.From(currentTenant.Id);
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

            var events = await securityEventRepository.ListByTenantAsync(
                tenantId,
                request.EventType,
                page,
                pageSize,
                cancellationToken);

            var unreviewedCount = await securityEventRepository.CountUnreviewedByTenantAsync(
                tenantId,
                cancellationToken);

            var items = events
                .Select(x => new SecurityEventResponse(
                    x.Id.Value,
                    x.EventType,
                    x.Description,
                    x.TenantId.Value,
                    x.UserId?.Value,
                    x.SessionId?.Value,
                    x.RiskScore,
                    x.IsReviewed,
                    x.ReviewedAt,
                    x.ReviewedBy?.Value,
                    x.OccurredAt,
                    x.MetadataJson))
                .ToList();

            return new Response(items, page, pageSize, unreviewedCount);
        }
    }

    /// <summary>Representação de evento de segurança para investigação operacional.</summary>
    public sealed record SecurityEventResponse(
        Guid SecurityEventId,
        string EventType,
        string Description,
        Guid TenantId,
        Guid? UserId,
        Guid? SessionId,
        int RiskScore,
        bool IsReviewed,
        DateTimeOffset? ReviewedAt,
        Guid? ReviewedBy,
        DateTimeOffset OccurredAt,
        string? MetadataJson);

    /// <summary>Envelope de resposta paginada com contagem de eventos pendentes.</summary>
    public sealed record Response(
        IReadOnlyList<SecurityEventResponse> Events,
        int Page,
        int PageSize,
        int UnreviewedCount);
}
