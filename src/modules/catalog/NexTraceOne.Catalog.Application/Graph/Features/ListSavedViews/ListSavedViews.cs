using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;

namespace NexTraceOne.EngineeringGraph.Application.Features.ListSavedViews;

/// <summary>
/// Feature: ListSavedViews — lista visões salvas do grafo para o usuário corrente.
/// Inclui visões próprias e visões compartilhadas dentro do tenant.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class ListSavedViews
{
    /// <summary>Query para listar visões salvas do usuário.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>
    /// Handler que lista visões salvas do grafo para o usuário atual.
    /// Retorna visões próprias e compartilhadas para permitir navegação rápida.
    /// </summary>
    public sealed class Handler(
        ISavedGraphViewRepository viewRepository,
        ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var views = await viewRepository.ListByOwnerAsync(currentUser.Id, cancellationToken);

            var items = views.Select(v => new SavedViewItem(
                v.Id.Value,
                v.Name,
                v.Description,
                v.OwnerId,
                v.IsShared,
                v.CreatedAt)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta com a lista de visões salvas.</summary>
    public sealed record Response(IReadOnlyList<SavedViewItem> Items);

    /// <summary>Resumo de uma visão salva do grafo.</summary>
    public sealed record SavedViewItem(
        Guid ViewId,
        string Name,
        string Description,
        string OwnerId,
        bool IsShared,
        DateTimeOffset CreatedAt);
}
