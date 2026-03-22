using Ardalis.GuardClauses;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.ListAuditCampaigns;

/// <summary>
/// Feature: ListAuditCampaigns — lista campanhas de auditoria com filtro opcional de status.
/// </summary>
public static class ListAuditCampaigns
{
    /// <summary>Query de listagem de campanhas de auditoria.</summary>
    public sealed record Query(CampaignStatus? Status) : IQuery<Response>;

    /// <summary>Handler que lista as campanhas de auditoria.</summary>
    public sealed class Handler(IAuditCampaignRepository auditCampaignRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var campaigns = await auditCampaignRepository.ListAsync(request.Status, cancellationToken);

            var items = campaigns
                .Select(c => new AuditCampaignItem(
                    c.Id.Value, c.Name, c.CampaignType, c.Status,
                    c.ScheduledStartAt, c.StartedAt, c.CompletedAt, c.CreatedAt))
                .ToArray();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de campanhas.</summary>
    public sealed record Response(IReadOnlyList<AuditCampaignItem> Items);

    /// <summary>Item de campanha de auditoria.</summary>
    public sealed record AuditCampaignItem(
        Guid CampaignId, string Name, string CampaignType, CampaignStatus Status,
        DateTimeOffset? ScheduledStartAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, DateTimeOffset CreatedAt);
}
