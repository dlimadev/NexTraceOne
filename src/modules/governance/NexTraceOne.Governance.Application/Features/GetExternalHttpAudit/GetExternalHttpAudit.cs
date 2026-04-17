using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetExternalHttpAudit;

/// <summary>
/// Feature: GetExternalHttpAudit — auditoria de chamadas HTTP externas da plataforma.
/// Integração com Elasticsearch pendente. Retorna lista vazia com SimulatedNote.
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
    public sealed class Handler : IQueryHandler<Query, ExternalHttpAuditResponse>
    {
        public Task<Result<ExternalHttpAuditResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = new ExternalHttpAuditResponse(
                Entries: [],
                Total: 0,
                Page: request.Page,
                PageSize: request.PageSize,
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: "Real HTTP audit data requires Elasticsearch integration. This endpoint will return live data once the analytics pipeline is configured.");

            return Task.FromResult(Result<ExternalHttpAuditResponse>.Success(response));
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
