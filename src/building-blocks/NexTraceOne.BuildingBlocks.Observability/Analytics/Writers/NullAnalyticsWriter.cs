using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Events;

namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Writers;

/// <summary>
/// Implementação nula do IAnalyticsWriter.
/// Usada quando Analytics:Enabled = false ou quando ClickHouse não está disponível.
/// Todas as operações retornam imediatamente sem I/O — graceful degradation.
/// O domínio transacional funciona normalmente sem dependência da camada analítica.
/// </summary>
public sealed class NullAnalyticsWriter : IAnalyticsWriter
{
    /// <inheritdoc />
    public Task WriteProductEventAsync(ProductAnalyticsRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteProductEventsBatchAsync(IReadOnlyList<ProductAnalyticsRecord> records, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteRuntimeMetricAsync(RuntimeMetricRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteCostEntryAsync(CostEntryRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteIncidentTrendEventAsync(IncidentTrendRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteIntegrationExecutionAsync(IntegrationExecutionRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteConnectorHealthEventAsync(ConnectorHealthRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteComplianceTrendAsync(ComplianceTrendRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteFinOpsAggregateAsync(FinOpsAggregateRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task WriteTraceReleaseMappingAsync(TraceReleaseMappingRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
