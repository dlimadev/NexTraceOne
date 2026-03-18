using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.SearchData;

/// <summary>
/// Feature: SearchData — pesquisa dados estruturados para grounding de IA.
/// Utiliza o IDatabaseRetrievalService para busca governada em fontes internas.
/// </summary>
public static class SearchData
{
    public sealed record Command(
        string Query,
        string? EntityType,
        string? TenantId,
        int? MaxResults) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Query).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.MaxResults).GreaterThan(0).When(x => x.MaxResults.HasValue);
        }
    }

    public sealed class Handler(
        IDatabaseRetrievalService databaseRetrievalService) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var searchRequest = new DatabaseSearchRequest(
                request.Query,
                request.EntityType,
                request.TenantId,
                request.MaxResults ?? 10);

            var searchResult = await databaseRetrievalService.SearchAsync(searchRequest, cancellationToken);

            if (!searchResult.Success)
            {
                return Error.Business(
                    "AI.DatabaseSearchFailed",
                    searchResult.ErrorMessage ?? "Database search failed.");
            }

            var hits = searchResult.Hits.Select(h => new DataHit(
                h.EntityType,
                h.EntityId,
                h.DisplayName,
                h.Summary,
                h.RelevanceScore)).ToList();

            return new Response(true, hits, hits.Count);
        }
    }

    public sealed record Response(
        bool Success,
        IReadOnlyList<DataHit> Hits,
        int TotalCount);

    public sealed record DataHit(
        string EntityType,
        string EntityId,
        string DisplayName,
        string Summary,
        double RelevanceScore);
}
