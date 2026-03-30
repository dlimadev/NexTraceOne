using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Features.ListServiceLinks;

/// <summary>
/// Feature: ListServiceLinks — lista todos os links de um serviço, ordenados por categoria e sort order.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListServiceLinks
{
    /// <summary>Query para listar links de um serviço.</summary>
    public sealed record Query(Guid ServiceAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de links.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que lista links de um serviço.</summary>
    public sealed class Handler(
        IServiceLinkRepository serviceLinkRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var serviceId = ServiceAssetId.From(request.ServiceAssetId);
            var links = await serviceLinkRepository.ListByServiceAsync(serviceId, cancellationToken);

            var items = links.Select(l => new LinkItem(
                l.Id.Value,
                l.ServiceAssetId.Value,
                l.Category.ToString(),
                l.Title,
                l.Url,
                l.Description,
                l.IconHint,
                l.SortOrder,
                l.CreatedAt)).ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de links de um serviço.</summary>
    public sealed record Response(IReadOnlyList<LinkItem> Items, int TotalCount);

    /// <summary>Representação de um link individual na resposta.</summary>
    public sealed record LinkItem(
        Guid LinkId,
        Guid ServiceAssetId,
        string Category,
        string Title,
        string Url,
        string Description,
        string IconHint,
        int SortOrder,
        DateTimeOffset CreatedAt);
}
