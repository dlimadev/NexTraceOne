using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;

// IMPLEMENTATION STATUS: Implemented in OperationalIntelligence.Infrastructure.TelemetryStore.Services.
// Registered via TelemetryStoreDependencyInjection.AddTelemetryStoreInfrastructure().

/// <summary>
/// Serviço de retenção que gerencia o ciclo de vida dos dados de telemetria.
/// Responsável por:
/// 1. Consolidar métricas de minuto para hora (aggregation rollup)
/// 2. Remover dados expirados conforme política de retenção
/// 3. Notificar backends de Telemetry Store para cleanup de dados crus
///
/// Executado como background job periódico via Quartz.NET.
/// Cada execução processa um tipo de dado por vez para não sobrecarregar o banco.
/// </summary>
public interface ITelemetryRetentionService
{
    /// <summary>
    /// Consolida métricas de minuto para métricas por hora.
    /// Agrega dados de service_metrics_1m para service_metrics_1h,
    /// calculando: sum, avg, p95, p99, max a partir dos snapshots de minuto.
    /// Execução típica: a cada hora, processa a hora anterior completa.
    /// </summary>
    Task ConsolidateMinuteToHourlyAsync(
        DateTimeOffset targetHour,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove métricas de minuto expiradas conforme política de retenção.
    /// Usa DROP PARTITION quando particionamento está ativo (muito mais eficiente que DELETE).
    /// </summary>
    Task PurgeExpiredMinuteMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove métricas por hora expiradas conforme política de retenção.
    /// </summary>
    Task PurgeExpiredHourlyMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove snapshots de anomalia expirados conforme política de retenção.
    /// Anomalias resolvidas há mais tempo que o TTL são removidas.
    /// Anomalias abertas nunca são removidas automaticamente.
    /// </summary>
    Task PurgeExpiredAnomalySnapshotsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove referências de telemetria expiradas.
    /// Referências cujo dado original já expirou no Telemetry Store são removidas.
    /// </summary>
    Task PurgeExpiredTelemetryReferencesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove topologia observada obsoleta (não vista há mais tempo que o TTL).
    /// Arestas de comunicação que não foram observadas recentemente são removidas.
    /// </summary>
    Task PurgeStaleTopologyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa todas as operações de retenção em sequência.
    /// Chamado pelo background job periódico.
    /// </summary>
    Task ExecuteFullRetentionCycleAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Serviço de agregação que consolida métricas de telemetria em intervalos maiores.
/// Responsável por computar indicadores agregados (req/min, req/h, throughput,
/// error rate, latência, p95, p99, CPU, memória) a partir de dados por minuto.
///
/// Padrão: métricas chegam ao Product Store no nível de 1 minuto via Collector connector,
/// e são consolidadas para 1 hora e 1 dia pelo job de agregação.
/// </summary>
public interface ITelemetryAggregationService
{
    /// <summary>
    /// Agrega métricas de minuto de todos os serviços para uma hora específica.
    /// Calcula: sum, avg, min, max, p50, p95, p99 por serviço/ambiente.
    /// </summary>
    Task AggregateServiceMetricsAsync(
        DateTimeOffset targetHour,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega métricas de dependência de minuto para uma hora específica.
    /// </summary>
    Task AggregateDependencyMetricsAsync(
        DateTimeOffset targetHour,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza topologia observada com base nas dependências vistas na última hora.
    /// Incrementa contadores, atualiza LastSeenAt, recalcula ConfidenceScore.
    /// </summary>
    Task UpdateObservedTopologyAsync(
        DateTimeOffset targetHour,
        CancellationToken cancellationToken = default);
}
