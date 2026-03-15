using Ardalis.GuardClauses;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.ListIdeCapabilityPolicies;

/// <summary>
/// Feature: ListIdeCapabilityPolicies — lista políticas de capacidade IDE.
/// Permite filtragem por tipo de cliente e estado.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListIdeCapabilityPolicies
{
    /// <summary>Query de listagem de políticas de capacidade IDE.</summary>
    public sealed record Query(
        string? ClientType,
        bool? IsActive,
        int PageSize = 50) : IQuery<Response>;

    /// <summary>Handler que lista políticas de capacidade IDE.</summary>
    public sealed class Handler(
        IAiIdeCapabilityPolicyRepository capabilityPolicyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var parsedClientType = request.ClientType is not null
                ? Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct) ? ct : (AIClientType?)null
                : null;

            var policies = await capabilityPolicyRepository.ListAsync(
                parsedClientType,
                request.IsActive,
                request.PageSize,
                cancellationToken);

            var items = policies
                .Select(p => new PolicyItem(
                    p.Id.Value,
                    p.ClientType.ToString(),
                    p.Persona,
                    p.AllowedCommands,
                    p.AllowedContextScopes,
                    p.AllowContractGeneration,
                    p.AllowIncidentTroubleshooting,
                    p.AllowExternalAI,
                    p.MaxTokensPerRequest,
                    p.IsActive))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de políticas de capacidade IDE.</summary>
    public sealed record Response(
        IReadOnlyList<PolicyItem> Items,
        int TotalCount);

    /// <summary>Item resumido de uma política de capacidade IDE.</summary>
    public sealed record PolicyItem(
        Guid PolicyId,
        string ClientType,
        string? Persona,
        string AllowedCommands,
        string AllowedContextScopes,
        bool AllowContractGeneration,
        bool AllowIncidentTroubleshooting,
        bool AllowExternalAI,
        int MaxTokensPerRequest,
        bool IsActive);
}
