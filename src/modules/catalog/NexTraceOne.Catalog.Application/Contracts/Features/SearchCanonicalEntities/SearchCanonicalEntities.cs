using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SearchCanonicalEntities;

/// <summary>
/// Feature: SearchCanonicalEntities — pesquisa entidades canónicas com filtros opcionais.
/// Permite localizar schemas reutilizáveis por nome, domínio, categoria e texto livre.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SearchCanonicalEntities
{
    /// <summary>Query de pesquisa de entidades canónicas com paginação.</summary>
    public sealed record Query(
        string? SearchTerm,
        string? Domain,
        string? Category,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de pesquisa.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que pesquisa entidades canónicas com filtros e paginação.</summary>
    public sealed class Handler(ICanonicalEntityRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (items, totalCount) = await repository.SearchAsync(
                request.SearchTerm,
                request.Domain,
                request.Category,
                request.Page,
                request.PageSize,
                cancellationToken);

            var summaries = items
                .Select(e => new CanonicalEntitySummary(
                    e.Id.Value,
                    e.Name,
                    e.Description,
                    e.Domain,
                    e.Category,
                    e.Owner,
                    e.Version,
                    e.State.ToString(),
                    e.SchemaFormat,
                    e.Criticality,
                    e.ReusePolicy,
                    e.KnownUsageCount,
                    e.CreatedAt))
                .ToList()
                .AsReadOnly();

            return new Response(summaries, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>Resumo de uma entidade canónica para listagem.</summary>
    public sealed record CanonicalEntitySummary(
        Guid Id,
        string Name,
        string Description,
        string Domain,
        string Category,
        string Owner,
        string Version,
        string State,
        string SchemaFormat,
        string Criticality,
        string ReusePolicy,
        int KnownUsageCount,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada da pesquisa de entidades canónicas.</summary>
    public sealed record Response(
        IReadOnlyList<CanonicalEntitySummary> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
