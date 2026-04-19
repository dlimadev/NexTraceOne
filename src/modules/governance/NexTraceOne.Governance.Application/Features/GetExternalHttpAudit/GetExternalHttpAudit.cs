using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetExternalHttpAudit;

/// <summary>
/// Feature: GetExternalHttpAudit — auditoria de chamadas HTTP externas da plataforma.
/// Consulta o IHttpAuditReader que delega para o IObservabilityProvider (Elastic/ClickHouse).
/// Fallback gracioso para lista vazia com SimulatedNote quando observabilidade não está configurada.
/// </summary>
public static class GetExternalHttpAudit
{
    /// <summary>Query com filtros opcionais para auditoria de HTTP externo.</summary>
    public sealed record Query(
        string? Destination,
        string? Context,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int Page,
        int PageSize) : IQuery<ExternalHttpAuditResponse>;

    /// <summary>Handler de listagem de auditoria HTTP externa.</summary>
    public sealed class Handler(IHttpAuditReader auditReader) : IQueryHandler<Query, ExternalHttpAuditResponse>
    {
        public async Task<Result<ExternalHttpAuditResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var filter = new HttpAuditFilter(
                Destination: request.Destination,
                Context: request.Context,
                From: request.From,
                To: request.To,
                Page: request.Page,
                PageSize: request.PageSize);

            var page = await auditReader.QueryAsync(filter, cancellationToken);

            var entries = page.Entries
                .Select(e => new ExternalHttpAuditEntryDto(
                    Id: e.Id,
                    Destination: e.Destination,
                    Method: e.Method,
                    StatusCode: e.StatusCode,
                    DurationMs: e.DurationMs,
                    Context: e.Context,
                    OccurredAt: e.OccurredAt))
                .ToList();

            var simulatedNote = page.IsLiveData
                ? string.Empty
                : "HTTP audit data requires Elasticsearch integration. Configure Telemetry:ObservabilityProvider:Provider=Elastic to enable live data.";

            var response = new ExternalHttpAuditResponse(
                Entries: entries,
                Total: page.Total,
                Page: request.Page,
                PageSize: request.PageSize,
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: simulatedNote);

            return Result<ExternalHttpAuditResponse>.Success(response);
        }
    }

    /// <summary>Resposta de auditoria HTTP externa com paginação.</summary>
    public sealed record ExternalHttpAuditResponse(
        IReadOnlyList<ExternalHttpAuditEntryDto> Entries,
        int Total,
        int Page,
        int PageSize,
        DateTimeOffset GeneratedAt,
        string SimulatedNote);

    /// <summary>Entrada de auditoria de chamada HTTP externa.</summary>
    public sealed record ExternalHttpAuditEntryDto(
        string Id,
        string Destination,
        string Method,
        int StatusCode,
        long DurationMs,
        string Context,
        DateTimeOffset OccurredAt);
}
