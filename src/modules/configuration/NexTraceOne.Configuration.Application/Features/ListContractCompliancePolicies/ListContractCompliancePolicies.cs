using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListContractCompliancePolicies;

/// <summary>
/// Feature: ListContractCompliancePolicies — lista políticas de compliance contratual
/// do tenant, com filtro opcional por âmbito.
/// Estrutura VSA: Query + Handler + Response em arquivo único.
/// </summary>
public static class ListContractCompliancePolicies
{
    /// <summary>Query de listagem de políticas de compliance contratual.</summary>
    public sealed record Query(int? Scope) : IQuery<Response>;

    /// <summary>
    /// Handler que lista políticas de compliance contratual por tenant.
    /// Filtra por âmbito quando especificado.
    /// </summary>
    public sealed class Handler(
        IContractCompliancePolicyRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var policies = await repository.ListByTenantAsync(
                currentTenant.Id.ToString(), cancellationToken);

            var items = policies
                .Where(p => request.Scope is null || (int)p.Scope == request.Scope)
                .Select(p => new PolicySummary(
                    p.Id.Value,
                    p.Name,
                    p.Description,
                    p.Scope.ToString(),
                    p.ScopeId,
                    p.IsActive,
                    p.VerificationMode.ToString(),
                    p.OnBreakingChange.ToString(),
                    p.CreatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resumo de uma política de compliance para listagem.</summary>
    public sealed record PolicySummary(
        Guid PolicyId,
        string Name,
        string Description,
        string Scope,
        string? ScopeId,
        bool IsActive,
        string VerificationMode,
        string OnBreakingChange,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta da listagem de políticas de compliance contratual.</summary>
    public sealed record Response(IReadOnlyList<PolicySummary> Items, int TotalCount);
}
