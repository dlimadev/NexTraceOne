using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Events;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Analytics;

/// <summary>
/// Implementação de <see cref="ITraceCorrelationWriter"/> que delega para o
/// <see cref="IAnalyticsWriter"/> do building block de observabilidade.
///
/// Quando Analytics:Enabled = false, o IAnalyticsWriter resolvido é o NullAnalyticsWriter,
/// garantindo graceful degradation sem alteração neste adapter.
/// </summary>
internal sealed class TraceCorrelationAnalyticsWriter(IAnalyticsWriter analyticsWriter) : ITraceCorrelationWriter
{
    /// <inheritdoc />
    public Task WriteAsync(
        Guid mappingId,
        Guid tenantId,
        Guid releaseId,
        string traceId,
        string serviceName,
        Guid? serviceId,
        string environment,
        Guid? environmentId,
        string correlationSource,
        DateTimeOffset? traceStartedAt,
        DateTimeOffset? traceEndedAt,
        DateTimeOffset correlatedAt,
        CancellationToken cancellationToken = default)
    {
        var record = new TraceReleaseMappingRecord(
            Id: mappingId,
            TenantId: tenantId,
            ReleaseId: releaseId,
            TraceId: traceId,
            ServiceName: serviceName,
            ServiceId: serviceId,
            Environment: environment,
            EnvironmentId: environmentId,
            CorrelationSource: correlationSource,
            TraceStartedAt: traceStartedAt,
            TraceEndedAt: traceEndedAt,
            CorrelatedAt: correlatedAt);

        return analyticsWriter.WriteTraceReleaseMappingAsync(record, cancellationToken);
    }
}
