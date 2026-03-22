using Ardalis.GuardClauses;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.ListComplianceResults;

/// <summary>
/// Feature: ListComplianceResults — lista resultados de compliance com filtros opcionais.
/// </summary>
public static class ListComplianceResults
{
    /// <summary>Query de listagem de resultados de compliance.</summary>
    public sealed record Query(Guid? PolicyId, Guid? CampaignId, ComplianceOutcome? Outcome) : IQuery<Response>;

    /// <summary>Handler que lista os resultados de compliance.</summary>
    public sealed class Handler(IComplianceResultRepository complianceResultRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policyId = request.PolicyId.HasValue ? CompliancePolicyId.From(request.PolicyId.Value) : null;
            var campaignId = request.CampaignId.HasValue ? AuditCampaignId.From(request.CampaignId.Value) : null;

            var results = await complianceResultRepository.ListAsync(policyId, campaignId, request.Outcome, cancellationToken);

            var items = results
                .Select(r => new ComplianceResultItem(
                    r.Id.Value, r.PolicyId.Value, r.CampaignId?.Value,
                    r.ResourceType, r.ResourceId, r.Outcome,
                    r.EvaluatedBy, r.EvaluatedAt))
                .ToArray();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de resultados.</summary>
    public sealed record Response(IReadOnlyList<ComplianceResultItem> Items);

    /// <summary>Item de resultado de compliance.</summary>
    public sealed record ComplianceResultItem(
        Guid ResultId, Guid PolicyId, Guid? CampaignId,
        string ResourceType, string ResourceId, ComplianceOutcome Outcome,
        string EvaluatedBy, DateTimeOffset EvaluatedAt);
}
