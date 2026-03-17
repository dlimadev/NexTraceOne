using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Enums;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetPortalAnalytics;

/// <summary>
/// Feature: GetPortalAnalytics — retorna métricas de adoção e uso do Developer Portal.
/// Mede buscas, acessos, playground, code generation e subscrições.
/// </summary>
public static class GetPortalAnalytics
{
    /// <summary>Query para obter métricas de analytics do portal.</summary>
    public sealed record Query(int DaysBack = 30) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de analytics.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DaysBack).InclusiveBetween(1, 365);
        }
    }

    /// <summary>
    /// Handler que calcula métricas de analytics do portal.
    /// Agrega contagens por tipo de evento e termos de busca mais frequentes.
    /// </summary>
    public sealed class Handler(IPortalAnalyticsRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var since = DateTimeOffset.UtcNow.AddDays(-request.DaysBack);

            var searchCount = await repository.CountByTypeAsync(
                PortalEventType.Search.ToString(), since, cancellationToken);
            var apiViewCount = await repository.CountByTypeAsync(
                PortalEventType.ApiView.ToString(), since, cancellationToken);
            var playgroundCount = await repository.CountByTypeAsync(
                PortalEventType.PlaygroundExecution.ToString(), since, cancellationToken);
            var codegenCount = await repository.CountByTypeAsync(
                PortalEventType.CodeGeneration.ToString(), since, cancellationToken);
            var subscriptionCount = await repository.CountByTypeAsync(
                PortalEventType.SubscriptionCreated.ToString(), since, cancellationToken);

            var topSearches = await repository.GetTopSearchesAsync(10, since, cancellationToken);
            var topSearchTerms = topSearches
                .Where(e => e.SearchQuery is not null)
                .GroupBy(e => e.SearchQuery!)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new TopSearchTerm(g.Key, g.Count()))
                .ToList();

            return new Response(
                SearchCount: searchCount,
                ApiViewCount: apiViewCount,
                PlaygroundExecutionCount: playgroundCount,
                CodeGenerationCount: codegenCount,
                SubscriptionCreatedCount: subscriptionCount,
                TopSearchTerms: topSearchTerms,
                DaysBack: request.DaysBack);
        }
    }

    /// <summary>Termo de busca mais frequente no portal.</summary>
    public sealed record TopSearchTerm(string Term, int Count);

    /// <summary>Resposta com métricas de analytics do portal.</summary>
    public sealed record Response(
        int SearchCount,
        int ApiViewCount,
        int PlaygroundExecutionCount,
        int CodeGenerationCount,
        int SubscriptionCreatedCount,
        IReadOnlyList<TopSearchTerm> TopSearchTerms,
        int DaysBack);
}
