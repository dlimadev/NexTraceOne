using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;

// IMPLEMENTATION STATUS: Implemented — EF Core repositories in OperationalIntelligence.Infrastructure.TelemetryStore.
// ServiceMetricsRepository and DependencyMetricsRepository provide PostgreSQL-backed persistence.
// Registered in DI via AddTelemetryStoreInfrastructure().

/// <summary>
/// Porta de escrita do Product Store para métricas de serviço agregadas.
/// Implementação reside na Infrastructure do módulo OperationalIntelligence.
///
/// O Product Store (PostgreSQL) armazena apenas dados agregados por minuto e hora.
/// Dados crus ficam no provider de observabilidade (Elastic ou ClickHouse)
/// e são referenciados via TelemetryReference.
/// </summary>
public interface IServiceMetricsWriter
{
    /// <summary>
    /// Persiste um snapshot de métricas agregadas de serviço no Product Store.
    /// Deve ser chamado pelo job de consolidação após agregar dados do pipeline.
    /// </summary>
    Task WriteAsync(ServiceMetricsSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste um lote de snapshots de métricas de forma eficiente (bulk insert).
    /// Usado pelo job de consolidação batch que processa múltiplos serviços.
    /// </summary>
    Task WriteBatchAsync(IReadOnlyList<ServiceMetricsSnapshot> snapshots, CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de leitura do Product Store para métricas de serviço agregadas.
/// Alimenta dashboards, drift detection, cost analysis e AI investigation.
/// </summary>
public interface IServiceMetricsReader
{
    /// <summary>
    /// Busca métricas agregadas de um serviço em um intervalo de tempo.
    /// Seleciona automaticamente o nível de agregação adequado ao intervalo:
    /// - Até 6 horas: agregados por minuto
    /// - Até 30 dias: agregados por hora
    /// - Acima: agregados por dia
    /// </summary>
    Task<IReadOnlyList<ServiceMetricsSnapshot>> GetByServiceAsync(
        Guid serviceId,
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        AggregationLevel? level = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca o snapshot mais recente de métricas de um serviço.
    /// Usado para comparação rápida com baseline e detecção de anomalia.
    /// </summary>
    Task<ServiceMetricsSnapshot?> GetLatestAsync(
        Guid serviceId,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna top N serviços por uma métrica específica (throughput, error rate, latência).
    /// Usado para dashboards de "top dependencies", "top operations", "top errors".
    /// </summary>
    Task<IReadOnlyList<ServiceMetricsSnapshot>> GetTopServicesAsync(
        string environment,
        string orderByMetric,
        int top,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de escrita do Product Store para métricas de dependência agregadas.
/// Registra throughput, latência e error rate da comunicação entre serviços.
/// </summary>
public interface IDependencyMetricsWriter
{
    /// <summary>Persiste um snapshot de métricas de dependência.</summary>
    Task WriteAsync(DependencyMetricsSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Persiste um lote de snapshots de dependência (bulk insert).</summary>
    Task WriteBatchAsync(IReadOnlyList<DependencyMetricsSnapshot> snapshots, CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de leitura do Product Store para métricas de dependência.
/// Alimenta topologia observada, blast radius e cost intelligence.
/// </summary>
public interface IDependencyMetricsReader
{
    /// <summary>Busca métricas de dependência de um serviço (outgoing ou incoming).</summary>
    Task<IReadOnlyList<DependencyMetricsSnapshot>> GetByServiceAsync(
        Guid serviceId,
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna top N dependências por volume de chamadas ou error rate.</summary>
    Task<IReadOnlyList<DependencyMetricsSnapshot>> GetTopDependenciesAsync(
        string environment,
        string orderByMetric,
        int top,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}
