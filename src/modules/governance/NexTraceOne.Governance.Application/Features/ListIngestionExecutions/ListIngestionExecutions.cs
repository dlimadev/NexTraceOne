using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ListIngestionExecutions;

/// <summary>
/// Feature: ListIngestionExecutions — lista execuções de ingestão com filtros temporais e de resultado.
/// Permite rastrear histórico de execuções por conector, fonte, resultado e intervalo de tempo.
/// </summary>
public static class ListIngestionExecutions
{
    /// <summary>Query para listar execuções de ingestão com filtros e paginação.</summary>
    public sealed record Query(
        Guid? ConnectorId = null,
        Guid? SourceId = null,
        string? Result = null,
        DateTimeOffset? From = null,
        DateTimeOffset? To = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que retorna a lista paginada de execuções de ingestão.</summary>
    public sealed class Handler(
        IIngestionExecutionRepository executionRepository,
        IIntegrationConnectorRepository connectorRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Parse optional filters
            IntegrationConnectorId? connectorIdFilter = request.ConnectorId.HasValue
                ? new IntegrationConnectorId(request.ConnectorId.Value)
                : null;

            IngestionSourceId? sourceIdFilter = request.SourceId.HasValue
                ? new IngestionSourceId(request.SourceId.Value)
                : null;

            ExecutionResult? resultFilter = null;
            if (!string.IsNullOrEmpty(request.Result) &&
                Enum.TryParse<ExecutionResult>(request.Result, ignoreCase: true, out var parsedResult))
            {
                resultFilter = parsedResult;
            }

            var executions = await executionRepository.ListAsync(
                connectorId: connectorIdFilter,
                sourceId: sourceIdFilter,
                result: resultFilter,
                from: request.From,
                to: request.To,
                page: request.Page,
                pageSize: request.PageSize,
                ct: cancellationToken);

            var total = await executionRepository.CountAsync(
                connectorId: connectorIdFilter,
                sourceId: sourceIdFilter,
                result: resultFilter,
                from: request.From,
                to: request.To,
                ct: cancellationToken);

            // Get connector names for display
            var connectorIds = executions.Select(e => e.ConnectorId).Distinct().ToList();
            var connectorNames = new Dictionary<IntegrationConnectorId, string>();

            foreach (var connId in connectorIds)
            {
                var connector = await connectorRepository.GetByIdAsync(connId, cancellationToken);
                if (connector is not null)
                {
                    connectorNames[connId] = connector.Name;
                }
            }

            var items = executions.Select(e => new IngestionExecutionItem(
                ExecutionId: e.Id.Value,
                ConnectorId: e.ConnectorId.Value,
                ConnectorName: connectorNames.TryGetValue(e.ConnectorId, out var name) ? name : "Unknown",
                SourceId: e.SourceId?.Value,
                StartedAt: e.StartedAt,
                FinishedAt: e.CompletedAt,
                DurationMs: e.DurationMs ?? 0,
                Result: e.Result.ToString(),
                RecordsReceived: e.ItemsProcessed,
                RecordsProcessed: e.ItemsSucceeded,
                RecordsNormalized: e.ItemsSucceeded,
                Warnings: e.ItemsFailed > 0 && e.ItemsSucceeded > 0 ? e.ItemsFailed : 0,
                Errors: e.Result == ExecutionResult.Failed ? e.ItemsFailed : 0,
                RetryAttempt: e.RetryAttempt,
                CorrelationId: e.CorrelationId ?? ""))
                .ToList();

            var response = new Response(
                TotalCount: total,
                Page: request.Page,
                PageSize: request.PageSize,
                Items: items);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta paginada com lista de execuções de ingestão.</summary>
    public sealed record Response(
        int TotalCount,
        int Page,
        int PageSize,
        IReadOnlyList<IngestionExecutionItem> Items);

    /// <summary>DTO de uma execução de ingestão com métricas de processamento.</summary>
    public sealed record IngestionExecutionItem(
        Guid ExecutionId,
        Guid ConnectorId,
        string ConnectorName,
        Guid? SourceId,
        DateTimeOffset StartedAt,
        DateTimeOffset? FinishedAt,
        long DurationMs,
        string Result,
        long RecordsReceived,
        long RecordsProcessed,
        long RecordsNormalized,
        int Warnings,
        int Errors,
        int RetryAttempt,
        string CorrelationId);
}
