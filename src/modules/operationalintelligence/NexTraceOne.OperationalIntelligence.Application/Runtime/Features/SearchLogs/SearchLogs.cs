using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.SearchLogs;

/// <summary>
/// Feature: SearchLogs — pesquisa logs estruturados via backend de telemetria (Elasticsearch ou ClickHouse).
/// SaaS-07: Log Search UI.
///
/// Suporta filtros por serviço, severidade, ambiente e janela de tempo.
/// Devolve até PageSize entradas com atributos completos.
/// Usa ITelemetrySearchService como abstracção sobre o backend escolhido pelo usuário.
/// </summary>
public static class SearchLogs
{
    /// <summary>Janela de tempo pré-definida para pesquisa de logs.</summary>
    public enum TimeWindow
    {
        Last15Min,
        Last1Hour,
        Last6Hours,
        Last24Hours,
        Last7Days,
        Custom,
    }

    public sealed record Query(
        string? ServiceName = null,
        string? Severity = null,
        string? Environment = null,
        string? SearchText = null,
        TimeWindow Window = TimeWindow.Last1Hour,
        DateTimeOffset? From = null,
        DateTimeOffset? To = null,
        int Page = 1,
        int PageSize = 50) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.SearchText).MaximumLength(1000).When(x => x.SearchText is not null);
            RuleFor(x => x.From).LessThan(x => x.To)
                .When(x => x.Window == TimeWindow.Custom && x.From.HasValue && x.To.HasValue);
        }
    }

    public sealed record LogEntry(
        string Id,
        DateTimeOffset Timestamp,
        string Severity,
        string Message,
        string? ServiceName,
        string? Environment,
        IReadOnlyDictionary<string, object?> Attributes);

    public sealed record Response(
        IReadOnlyList<LogEntry> Entries,
        long TotalCount,
        int Page,
        int PageSize,
        DateTimeOffset SearchFrom,
        DateTimeOffset SearchTo);

    internal sealed class Handler(
        ITelemetrySearchService telemetrySearchService,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (from, to) = ResolveTimeWindow(request, clock.UtcNow);

            var searchRequest = new LogSearchRequest(
                TenantId: currentTenant.Id,
                ServiceName: request.ServiceName,
                Severity: request.Severity,
                Environment: request.Environment,
                SearchText: request.SearchText,
                From: from,
                To: to,
                Page: request.Page,
                PageSize: request.PageSize);

            var (entries, total) = await telemetrySearchService.SearchAsync(searchRequest, cancellationToken);

            return new Response(entries, total, request.Page, request.PageSize, from, to);
        }

        private static (DateTimeOffset from, DateTimeOffset to) ResolveTimeWindow(Query req, DateTimeOffset now)
            => req.Window switch
            {
                TimeWindow.Last15Min => (now.AddMinutes(-15), now),
                TimeWindow.Last1Hour => (now.AddHours(-1), now),
                TimeWindow.Last6Hours => (now.AddHours(-6), now),
                TimeWindow.Last24Hours => (now.AddHours(-24), now),
                TimeWindow.Last7Days => (now.AddDays(-7), now),
                TimeWindow.Custom when req.From.HasValue && req.To.HasValue => (req.From.Value, req.To.Value),
                _ => (now.AddHours(-1), now),
            };
    }
}
