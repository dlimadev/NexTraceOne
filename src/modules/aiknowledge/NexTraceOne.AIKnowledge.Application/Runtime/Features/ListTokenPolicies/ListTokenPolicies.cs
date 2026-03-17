using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.ListTokenPolicies;

/// <summary>
/// Feature: ListTokenPolicies — lista todas as políticas de quota de tokens registadas.
/// Utilizado para consulta de governança e configuração de limites por persona/tenant/provider.
/// </summary>
public static class ListTokenPolicies
{
    public sealed record Query() : IQuery<Response>;

    public sealed class Handler(
        IAiTokenQuotaPolicyRepository policyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policies = await policyRepository.GetAllAsync(cancellationToken);

            var items = policies.Select(p => new PolicyItem(
                p.Id.Value,
                p.Name,
                p.Description,
                p.Scope,
                p.ScopeValue,
                p.ProviderId,
                p.ModelId,
                p.MaxInputTokensPerRequest,
                p.MaxOutputTokensPerRequest,
                p.MaxTotalTokensPerRequest,
                p.MaxTokensPerDay,
                p.MaxTokensPerMonth,
                p.IsHardLimit,
                p.IsEnabled)).ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record Response(
        IReadOnlyList<PolicyItem> Items,
        int TotalCount);

    public sealed record PolicyItem(
        Guid Id,
        string Name,
        string? Description,
        string Scope,
        string? ScopeValue,
        string? ProviderId,
        string? ModelId,
        int? MaxInputTokensPerRequest,
        int? MaxOutputTokensPerRequest,
        int? MaxTotalTokensPerRequest,
        long? MaxTokensPerDay,
        long? MaxTokensPerMonth,
        bool IsHardLimit,
        bool IsEnabled);
}
