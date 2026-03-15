using Ardalis.GuardClauses;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.ListPolicies;

/// <summary>
/// Feature: ListPolicies — lista políticas de acesso de IA com filtros opcionais.
/// Permite filtrar por escopo e estado de ativação para gestão de governança.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListPolicies
{
    /// <summary>Query de listagem filtrada de políticas de acesso de IA.</summary>
    public sealed record Query(
        string? Scope,
        bool? IsActive) : IQuery<Response>;

    /// <summary>Handler que lista políticas de acesso com filtros opcionais.</summary>
    public sealed class Handler(
        IAiAccessPolicyRepository policyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policies = await policyRepository.ListAsync(
                request.Scope,
                request.IsActive,
                cancellationToken);

            var items = policies
                .Select(p => new PolicyItem(
                    p.Id.Value,
                    p.Name,
                    p.Description,
                    p.Scope,
                    p.ScopeValue,
                    p.AllowExternalAI,
                    p.InternalOnly,
                    p.MaxTokensPerRequest,
                    p.IsActive))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de políticas de acesso de IA.</summary>
    public sealed record Response(
        IReadOnlyList<PolicyItem> Items,
        int TotalCount);

    /// <summary>Item resumido de uma política na listagem de governança.</summary>
    public sealed record PolicyItem(
        Guid PolicyId,
        string Name,
        string Description,
        string Scope,
        string ScopeValue,
        bool AllowExternalAI,
        bool InternalOnly,
        int MaxTokensPerRequest,
        bool IsActive);
}
