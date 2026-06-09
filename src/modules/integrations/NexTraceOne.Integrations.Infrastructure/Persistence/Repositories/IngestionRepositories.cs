using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Events;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de IngestionSources usando EF Core.
/// </summary>
internal sealed class IngestionSourceRepository(IntegrationsDbContext context) : IIngestionSourceRepository
{
    public async Task<IReadOnlyList<IngestionSource>> ListAsync(
        IntegrationConnectorId? connectorId,
        SourceStatus? status,
        FreshnessStatus? freshnessStatus,
        CancellationToken ct)
    {
        var query = context.IngestionSources.AsQueryable();

        if (connectorId is not null)
            query = query.Where(s => s.ConnectorId == connectorId);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (freshnessStatus.HasValue)
            query = query.Where(s => s.FreshnessStatus == freshnessStatus.Value);

        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<IngestionSource>> ListByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        CancellationToken ct)
        => await context.IngestionSources
            .Where(s => s.ConnectorId == connectorId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IngestionSource?> GetByIdAsync(IngestionSourceId id, CancellationToken ct)
        => await context.IngestionSources.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IngestionSource?> GetByConnectorAndNameAsync(
        IntegrationConnectorId connectorId,
        string name,
        CancellationToken ct)
        => await context.IngestionSources
            .SingleOrDefaultAsync(s => s.ConnectorId == connectorId && s.Name == name, ct);

    public async Task AddAsync(IngestionSource source, CancellationToken ct)
        => await context.IngestionSources.AddAsync(source, ct);

    public Task UpdateAsync(IngestionSource source, CancellationToken ct)
    {
        context.IngestionSources.Update(source);
        return Task.CompletedTask;
    }

    public async Task<int> CountByFreshnessStatusAsync(FreshnessStatus status, CancellationToken ct)
        => await context.IngestionSources.CountAsync(s => s.FreshnessStatus == status, ct);
}

/// <summary>
/// Implementação do repositório de IngestionExecutions usando EF Core.
/// Fase 4: ao completar uma execução (estado terminal), replica o registo para ClickHouse
/// via IAnalyticsWriter para análise histórica de longa duração.
/// </summary>
internal sealed class IngestionExecutionRepository(
    IntegrationsDbContext context,
    IAnalyticsWriter analyticsWriter,
    ILogger<IngestionExecutionRepository> logger) : IIngestionExecutionRepository
{
    public async Task<IReadOnlyList<IngestionExecution>> ListAsync(
        IntegrationConnectorId? connectorId,
        IngestionSourceId? sourceId,
        ExecutionResult? result,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.IngestionExecutions.AsQueryable();

        if (connectorId is not null)
            query = query.Where(e => e.ConnectorId == connectorId);

        if (sourceId is not null)
            query = query.Where(e => e.SourceId == sourceId);

        if (result.HasValue)
            query = query.Where(e => e.Result == result.Value);

        if (from.HasValue)
            query = query.Where(e => e.StartedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartedAt <= to.Value);

        return await query
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(
        IntegrationConnectorId? connectorId,
        IngestionSourceId? sourceId,
        ExecutionResult? result,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        var query = context.IngestionExecutions.AsQueryable();

        if (connectorId is not null)
            query = query.Where(e => e.ConnectorId == connectorId);

        if (sourceId is not null)
            query = query.Where(e => e.SourceId == sourceId);

        if (result.HasValue)
            query = query.Where(e => e.Result == result.Value);

        if (from.HasValue)
            query = query.Where(e => e.StartedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartedAt <= to.Value);

        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<IngestionExecution>> ListByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        int limit,
        CancellationToken ct)
        => await context.IngestionExecutions
            .Where(e => e.ConnectorId == connectorId)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IngestionExecution?> GetByIdAsync(IngestionExecutionId id, CancellationToken ct)
        => await context.IngestionExecutions.SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IngestionExecution?> GetLastByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        CancellationToken ct)
        => await context.IngestionExecutions
            .Where(e => e.ConnectorId == connectorId)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(IngestionExecution execution, CancellationToken ct)
        => await context.IngestionExecutions.AddAsync(execution, ct);

    public Task UpdateAsync(IngestionExecution execution, CancellationToken ct)
    {
        context.IngestionExecutions.Update(execution);

        // Fase 4: replica execuções terminais para ClickHouse (fire-and-forget).
        if (execution.Result != ExecutionResult.Running)
            _ = ForwardToClickHouseAsync(execution, ct);

        return Task.CompletedTask;
    }

    private async Task ForwardToClickHouseAsync(IngestionExecution execution, CancellationToken ct)
    {
        try
        {
            var connector = await context.IntegrationConnectors
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == execution.ConnectorId, ct);

            if (connector is null) return;

            var record = new IntegrationExecutionRecord(
                Id: execution.Id.Value,
                TenantId: connector.TenantId ?? Guid.Empty,
                ConnectorId: execution.ConnectorId.Value,
                ConnectorName: connector.Name,
                ConnectorType: connector.ConnectorType,
                Provider: connector.Provider,
                SourceId: execution.SourceId?.Value,
                DataDomain: connector.ConnectorType,
                CorrelationId: execution.CorrelationId,
                StartedAt: execution.StartedAt,
                CompletedAt: execution.CompletedAt,
                DurationMs: execution.DurationMs,
                Result: execution.Result.ToString(),
                ItemsProcessed: execution.ItemsProcessed,
                ItemsSucceeded: execution.ItemsSucceeded,
                ItemsFailed: execution.ItemsFailed,
                ErrorCode: execution.ErrorCode,
                RetryAttempt: execution.RetryAttempt,
                CreatedAt: execution.CreatedAt);

            await analyticsWriter.WriteIntegrationExecutionAsync(record, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "Failed to forward IngestionExecution {ExecutionId} to ClickHouse — suppressed",
                execution.Id.Value);
        }
    }

    public async Task<int> CountByResultInPeriodAsync(
        ExecutionResult result,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => await context.IngestionExecutions
            .CountAsync(e => e.Result == result && e.StartedAt >= from && e.StartedAt <= to, ct);
}
