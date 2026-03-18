using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.SearchDocuments;

/// <summary>
/// Feature: SearchDocuments — pesquisa documentos para grounding de IA.
/// Utiliza o IDocumentRetrievalService para busca em fontes documentais.
/// </summary>
public static class SearchDocuments
{
    public sealed record Command(
        string Query,
        int? MaxResults,
        string? SourceFilter,
        string? ClassificationFilter) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Query).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.MaxResults).GreaterThan(0).When(x => x.MaxResults.HasValue);
        }
    }

    public sealed class Handler(
        IDocumentRetrievalService documentRetrievalService) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var searchRequest = new DocumentSearchRequest(
                request.Query,
                request.MaxResults ?? 10,
                request.SourceFilter,
                request.ClassificationFilter);

            var searchResult = await documentRetrievalService.SearchAsync(searchRequest, cancellationToken);

            if (!searchResult.Success)
            {
                return Error.Business(
                    "AI.DocumentSearchFailed",
                    searchResult.ErrorMessage ?? "Document search failed.");
            }

            var hits = searchResult.Hits.Select(h => new DocumentHit(
                h.SourceId,
                h.DocumentId,
                h.Title,
                h.Snippet,
                h.RelevanceScore,
                h.Classification)).ToList();

            return new Response(true, hits, hits.Count);
        }
    }

    public sealed record Response(
        bool Success,
        IReadOnlyList<DocumentHit> Hits,
        int TotalCount);

    public sealed record DocumentHit(
        string SourceId,
        string DocumentId,
        string Title,
        string Snippet,
        double RelevanceScore,
        string? Classification);
}
