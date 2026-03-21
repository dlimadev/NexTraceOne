using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgents;

/// <summary>
/// Feature: ListAgents — lista agents de IA registados e disponíveis ao utilizador.
/// Retorna agents oficiais e customizados ativos, agrupados por categoria.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListAgents
{
    /// <summary>Query para listar agents disponíveis.</summary>
    public sealed record Query(bool? IsOfficial) : IQuery<Response>;

    /// <summary>Handler que lista agents ativos disponíveis ao utilizador.</summary>
    public sealed class Handler(
        IAiAgentRepository agentRepository,
        ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var agents = await agentRepository.ListAsync(
                isActive: true,
                isOfficial: request.IsOfficial,
                cancellationToken);

            var items = agents
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.DisplayName)
                .Select(a => new AgentItem(
                    a.Id.Value,
                    a.Name,
                    a.DisplayName,
                    a.Slug,
                    a.Description,
                    a.Category.ToString(),
                    a.IsOfficial,
                    a.IsActive,
                    a.Capabilities,
                    a.TargetPersona,
                    a.Icon,
                    a.PreferredModelId,
                    a.OwnershipType.ToString(),
                    a.Visibility.ToString(),
                    a.PublicationStatus.ToString(),
                    a.Version,
                    a.ExecutionCount))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta com agents disponíveis.</summary>
    public sealed record Response(
        IReadOnlyList<AgentItem> Items,
        int TotalCount);

    /// <summary>Item de agent na listagem.</summary>
    public sealed record AgentItem(
        Guid AgentId,
        string Name,
        string DisplayName,
        string Slug,
        string Description,
        string Category,
        bool IsOfficial,
        bool IsActive,
        string Capabilities,
        string TargetPersona,
        string Icon,
        Guid? PreferredModelId,
        string OwnershipType,
        string Visibility,
        string PublicationStatus,
        int Version,
        long ExecutionCount);
}
