namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Abstração de escrita analítica para correlações trace → release.
/// Isola a camada Application do detalhe de infraestrutura (ClickHouse via IAnalyticsWriter).
///
/// Implementado em NexTraceOne.ChangeGovernance.Infrastructure via ClickHouse IAnalyticsWriter.
/// Quando ClickHouse está desativado, a implementação é no-op (NullTraceCorrelationWriter).
/// </summary>
public interface ITraceCorrelationWriter
{
    /// <summary>
    /// Regista uma correlação trace → release no storage analítico (ClickHouse).
    /// O registo é append-only e a falha é suprimida — nunca bloqueia o fluxo de domínio.
    /// </summary>
    Task WriteAsync(
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
        CancellationToken cancellationToken = default);
}
