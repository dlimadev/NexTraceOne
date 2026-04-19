using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListSelfHealingActions;

/// <summary>
/// Feature: ListSelfHealingActions — lista acções de auto-remediação com filtros.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListSelfHealingActions
{
    public sealed record Query(Guid TenantId, string? IncidentId, bool PendingOnly) : IQuery<Response>;

    public sealed class Handler(ISelfHealingActionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            IReadOnlyList<Domain.Governance.Entities.SelfHealingAction> actions;

            if (request.PendingOnly)
                actions = await repository.ListPendingApprovalAsync(request.TenantId, ct);
            else if (!string.IsNullOrWhiteSpace(request.IncidentId))
                actions = await repository.ListByIncidentAsync(request.IncidentId, request.TenantId, ct);
            else
            {
                var pending = await repository.ListPendingApprovalAsync(request.TenantId, ct);
                actions = pending;
            }

            var summaries = actions.Select(a => new SelfHealingActionSummary(
                a.Id.Value,
                a.IncidentId,
                a.ServiceName,
                a.ActionType,
                a.ActionDescription,
                a.Confidence,
                a.RiskLevel,
                a.Status,
                a.ProposedAt)).ToList().AsReadOnly();

            return new Response(summaries);
        }
    }

    public sealed record Response(IReadOnlyList<SelfHealingActionSummary> Actions);

    public sealed record SelfHealingActionSummary(
        Guid ActionId,
        string IncidentId,
        string ServiceName,
        string ActionType,
        string ActionDescription,
        double Confidence,
        string RiskLevel,
        string Status,
        DateTimeOffset ProposedAt);
}
