using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListUserModelPolicies;

/// <summary>
/// Feature: ListUserModelPolicies — lista políticas de acesso a modelos por utilizador (scope = "user").
/// </summary>
public static class ListUserModelPolicies
{
    /// <summary>Query de listagem de políticas por utilizador.</summary>
    public sealed record Query(bool? IsActive) : IQuery<Response>;

    /// <summary>Handler que lista políticas de acesso de todos os utilizadores do tenant.</summary>
    public sealed class Handler(
        IAiAccessPolicyRepository policyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policies = await policyRepository.ListAsync(
                scope: "user",
                isActive: request.IsActive,
                ct: cancellationToken);

            var items = policies.Select(p => new UserModelPolicyDto(
                p.Id.Value,
                p.ScopeValue,
                p.Name,
                p.Description,
                p.AllowedModelIds,
                p.BlockedModelIds,
                p.AllowExternalAI,
                p.InternalOnly,
                p.MaxTokensPerRequest,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de políticas por utilizador.</summary>
    public sealed record Response(IReadOnlyList<UserModelPolicyDto> Items);

    /// <summary>DTO de política de acesso a modelos por utilizador.</summary>
    public sealed record UserModelPolicyDto(
        Guid PolicyId,
        string UserId,
        string PolicyName,
        string Description,
        string AllowedModelIds,
        string BlockedModelIds,
        bool AllowExternalAI,
        bool InternalOnly,
        int MaxTokensPerRequest,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
