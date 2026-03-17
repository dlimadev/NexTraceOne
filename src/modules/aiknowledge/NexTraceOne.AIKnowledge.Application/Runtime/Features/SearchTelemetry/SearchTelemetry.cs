using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.SearchTelemetry;

/// <summary>
/// Feature: SearchTelemetry — pesquisa dados de telemetria para grounding de IA.
/// Utiliza o ITelemetryRetrievalService para busca em traces, logs e métricas.
/// </summary>
public static class SearchTelemetry
{
    public sealed record Command(
        string Query,
        string? TraceId,
        string? SpanId,
        string? ServiceName,
        string? Severity,
        DateTimeOffset? From,
        DateTimeOffset? To,
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
        ITelemetryRetrievalService telemetryRetrievalService) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var searchRequest = new TelemetrySearchRequest(
                request.Query,
                request.TraceId,
                request.SpanId,
                request.ServiceName,
                request.Severity,
                request.From,
                request.To,
                request.MaxResults ?? 50);

            var searchResult = await telemetryRetrievalService.SearchAsync(searchRequest, cancellationToken);

            if (!searchResult.Success)
            {
                return Error.Business(
                    "AI.TelemetrySearchFailed",
                    searchResult.ErrorMessage ?? "Telemetry search failed.");
            }

            var hits = searchResult.Hits.Select(h => new TelemetryHit(
                h.TraceId,
                h.SpanId,
                h.ServiceName,
                h.Message,
                h.Severity,
                h.Timestamp,
                h.DurationMs)).ToList();

            return new Response(true, hits, hits.Count);
        }
    }

    public sealed record Response(
        bool Success,
        IReadOnlyList<TelemetryHit> Hits,
        int TotalCount);

    public sealed record TelemetryHit(
        string TraceId,
        string? SpanId,
        string ServiceName,
        string Message,
        string Severity,
        DateTimeOffset Timestamp,
        double? DurationMs);
}
