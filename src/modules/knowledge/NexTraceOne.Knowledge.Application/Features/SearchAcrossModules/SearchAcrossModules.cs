using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application.Features.SearchAcrossModules;

/// <summary>
/// Feature: SearchAcrossModules — pesquisa unificada sobre documentos do Knowledge Hub,
/// runbooks propostos e notas operacionais. Fornece ponto único de entrada para busca
/// de conhecimento operacional no NexTraceOne.
/// Pilar: Source of Truth. Owner: Knowledge.
/// </summary>
public static class SearchAcrossModules
{
    public sealed record Query(string Term, int MaxResults = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Term).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxResults).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(
        IKnowledgeDocumentRepository docRepo,
        IOperationalNoteRepository noteRepo,
        IProposedRunbookRepository runbookRepo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxPerSource = Math.Max(5, request.MaxResults / 3);

            var docs = await docRepo.SearchAsync(request.Term, maxPerSource, cancellationToken);
            var notes = await noteRepo.SearchAsync(request.Term, maxPerSource, cancellationToken);
            var runbooks = await runbookRepo.ListAsync(serviceName: null, ct: cancellationToken);

            var matchingRunbooks = runbooks
                .Where(r => r.Title.Contains(request.Term, StringComparison.OrdinalIgnoreCase)
                    || r.ContentMarkdown.Contains(request.Term, StringComparison.OrdinalIgnoreCase))
                .Take(maxPerSource)
                .ToList();

            var results = new List<SearchResultDto>();

            results.AddRange(docs.Select(d => new SearchResultDto(
                Id: d.Id.Value.ToString(),
                Title: d.Title,
                Summary: d.Summary ?? string.Empty,
                Module: "Knowledge",
                Type: "Document",
                Score: 1.0m)));

            results.AddRange(notes.Select(n => new SearchResultDto(
                Id: n.Id.Value.ToString(),
                Title: n.Title,
                Summary: n.Content.Length > 200 ? n.Content[..200] + "..." : n.Content,
                Module: "Knowledge",
                Type: "OperationalNote",
                Score: 0.9m)));

            results.AddRange(matchingRunbooks.Select(r => new SearchResultDto(
                Id: r.Id.Value.ToString(),
                Title: r.Title,
                Summary: $"Proposed runbook from incident. Status: {r.Status}",
                Module: "Knowledge",
                Type: "ProposedRunbook",
                Score: 0.8m)));

            return Result<Response>.Success(new Response(
                Term: request.Term,
                TotalResults: results.Count,
                Results: results.OrderByDescending(r => r.Score).Take(request.MaxResults).ToList()));
        }
    }

    public sealed record Response(string Term, int TotalResults, IReadOnlyList<SearchResultDto> Results);

    public sealed record SearchResultDto(string Id, string Title, string Summary, string Module, string Type, decimal Score);
}
