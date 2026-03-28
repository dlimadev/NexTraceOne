using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPlatformEvents;

/// <summary>
/// Feature: GetPlatformEvents — eventos operacionais recentes derivados de atividade de governança.
/// Fontes reais: rollouts aplicados/falhados e waivers aprovados/rejeitados persistidos na base de dados.
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

    /// <summary>Handler que retorna eventos operacionais reais via IPlatformEventProvider.</summary>
    public sealed class Handler(IPlatformEventProvider eventProvider) : IQueryHandler<Query, Response>
    {
        private const int MaxEventLoad = 200;

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var rawEvents = await eventProvider.GetRecentEventsAsync(MaxEventLoad, cancellationToken);

            var filtered = rawEvents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.SeverityFilter)
                && Enum.TryParse<PlatformEventSeverity>(request.SeverityFilter, ignoreCase: true, out var severity))
            {
                filtered = filtered.Where(e => e.Severity.Equals(severity.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.SubsystemFilter))
            {
                filtered = filtered.Where(e =>
                    e.Subsystem.Equals(request.SubsystemFilter, StringComparison.OrdinalIgnoreCase));
            }

            var page = Math.Max(1, request.Page ?? 1);
            var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);
            var items = filtered.ToList();
            var paged = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new PlatformOperationalEventDto(
                    EventId: e.EventId,
                    Timestamp: e.Timestamp,
                    Severity: Enum.TryParse<PlatformEventSeverity>(e.Severity, out var sev)
                        ? sev
                        : PlatformEventSeverity.Info,
                    Subsystem: e.Subsystem,
                    Message: e.Message,
                    CorrelationId: null,
                    Resolved: e.Resolved))
                .ToList();

            return Result<Response>.Success(new Response(
                Events: paged,
                TotalCount: items.Count,
                Page: page,
                PageSize: pageSize));
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
