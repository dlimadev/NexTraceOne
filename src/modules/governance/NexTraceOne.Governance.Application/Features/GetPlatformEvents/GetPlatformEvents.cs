using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPlatformEvents;

/// <summary>
/// Feature: GetPlatformEvents — eventos operacionais recentes da plataforma.
/// Permite filtragem por severidade e subsistema para triagem operacional rápida.
/// </summary>
public static class GetPlatformEvents
{
    /// <summary>Query com filtros opcionais de severidade, subsistema e paginação.</summary>
    public sealed record Query(
        string? SeverityFilter = null,
        string? SubsystemFilter = null,
        int? Page = null,
        int? PageSize = null) : IQuery<Response>;

    /// <summary>Handler que retorna eventos operacionais filtrados e paginados.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // TODO [P03.5]: Replace static platform events with real operational event stream
            // once platform events contract is available for Governance aggregation.
            var now = DateTimeOffset.UtcNow;

            var allEvents = new List<PlatformOperationalEventDto>
            {
                new(
                    EventId: "evt-001",
                    Timestamp: now.AddMinutes(-5),
                    Severity: PlatformEventSeverity.Info,
                    Subsystem: "API",
                    Message: "Rate limiter threshold adjusted for /api/v1/contracts endpoint.",
                    CorrelationId: "corr-abc-001",
                    Resolved: true),
                new(
                    EventId: "evt-002",
                    Timestamp: now.AddMinutes(-12),
                    Severity: PlatformEventSeverity.Warning,
                    Subsystem: "Ingestion",
                    Message: "Ingestion pipeline latency increased above 500ms threshold.",
                    CorrelationId: "corr-abc-002",
                    Resolved: true),
                new(
                    EventId: "evt-003",
                    Timestamp: now.AddMinutes(-30),
                    Severity: PlatformEventSeverity.Error,
                    Subsystem: "BackgroundJobs",
                    Message: "Outbox Processor failed to acquire lock after 3 retries.",
                    CorrelationId: "corr-abc-003",
                    Resolved: true),
                new(
                    EventId: "evt-004",
                    Timestamp: now.AddHours(-1),
                    Severity: PlatformEventSeverity.Info,
                    Subsystem: "Identity",
                    Message: "Identity token cache refreshed successfully.",
                    CorrelationId: "corr-abc-004",
                    Resolved: true),
                new(
                    EventId: "evt-005",
                    Timestamp: now.AddHours(-2),
                    Severity: PlatformEventSeverity.Critical,
                    Subsystem: "Database",
                    Message: "Read replica connection pool exhausted. Failover to primary initiated.",
                    CorrelationId: "corr-abc-005",
                    Resolved: true),
                new(
                    EventId: "evt-006",
                    Timestamp: now.AddHours(-3),
                    Severity: PlatformEventSeverity.Warning,
                    Subsystem: "AI",
                    Message: "AI model inference latency degraded above SLA threshold.",
                    CorrelationId: "corr-abc-006",
                    Resolved: false),
                new(
                    EventId: "evt-007",
                    Timestamp: now.AddHours(-4),
                    Severity: PlatformEventSeverity.Info,
                    Subsystem: "Governance",
                    Message: "Compliance check batch completed. 142 services evaluated.",
                    CorrelationId: "corr-abc-007",
                    Resolved: true)
            };

            // Aplicar filtro de severidade
            var filtered = allEvents.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.SeverityFilter)
                && Enum.TryParse<PlatformEventSeverity>(request.SeverityFilter, ignoreCase: true, out var severity))
            {
                filtered = filtered.Where(e => e.Severity == severity);
            }

            // Aplicar filtro de subsistema
            if (!string.IsNullOrWhiteSpace(request.SubsystemFilter))
            {
                filtered = filtered.Where(e =>
                    e.Subsystem.Equals(request.SubsystemFilter, StringComparison.OrdinalIgnoreCase));
            }

            var page = Math.Max(1, request.Page ?? 1);
            var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);
            var items = filtered.ToList();
            var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var response = new Response(
                Events: paged,
                TotalCount: items.Count,
                Page: page,
                PageSize: pageSize);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta paginada de eventos operacionais da plataforma.</summary>
    public sealed record Response(
        IReadOnlyList<PlatformOperationalEventDto> Events,
        int TotalCount,
        int Page,
        int PageSize);

    /// <summary>Evento operacional da plataforma com severidade, subsistema e estado de resolução.</summary>
    public sealed record PlatformOperationalEventDto(
        string EventId,
        DateTimeOffset Timestamp,
        PlatformEventSeverity Severity,
        string Subsystem,
        string Message,
        string? CorrelationId,
        bool Resolved);
}
