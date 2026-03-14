namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

/// <summary>
/// Snapshot de métricas agregadas de um serviço em um intervalo de tempo.
/// Armazenado no Product Store (PostgreSQL) em tabelas particionadas por tempo.
///
/// Tabelas-alvo:
/// - service_metrics_1m: agregados por minuto (retenção curta: 7 dias)
/// - service_metrics_1h: agregados por hora (retenção média: 90 dias)
///
/// Não contém dados crus — apenas indicadores consolidados que alimentam
/// dashboards, alertas, drift detection e investigação de anomalias.
/// </summary>
public sealed record ServiceMetricsSnapshot
{
    /// <summary>Identificador único do snapshot.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Identificador do serviço no catálogo (ApiAssetId ou ServiceAssetId).</summary>
    public required Guid ServiceId { get; init; }

    /// <summary>Nome do serviço para consulta rápida sem join.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Ambiente (production, staging, development).</summary>
    public required string Environment { get; init; }

    /// <summary>Tenant ID para isolamento multi-tenant.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Nível de agregação deste snapshot (1 minuto, 1 hora, 1 dia).</summary>
    public required AggregationLevel AggregationLevel { get; init; }

    /// <summary>Início do intervalo de agregação (UTC).</summary>
    public required DateTimeOffset IntervalStart { get; init; }

    /// <summary>Fim do intervalo de agregação (UTC).</summary>
    public required DateTimeOffset IntervalEnd { get; init; }

    // ── Throughput ─────────────────────────────────────────────────────

    /// <summary>Total de requisições no intervalo.</summary>
    public long RequestCount { get; init; }

    /// <summary>Requisições por minuto (calculado: RequestCount / minutos do intervalo).</summary>
    public double RequestsPerMinute { get; init; }

    /// <summary>Requisições por hora (calculado).</summary>
    public double RequestsPerHour { get; init; }

    // ── Error Rate ────────────────────────────────────────────────────

    /// <summary>Total de erros no intervalo (status 5xx, exceções).</summary>
    public long ErrorCount { get; init; }

    /// <summary>Taxa de erro percentual (ErrorCount / RequestCount * 100).</summary>
    public double ErrorRatePercent { get; init; }

    // ── Latência ──────────────────────────────────────────────────────

    /// <summary>Latência média em milissegundos.</summary>
    public double LatencyAvgMs { get; init; }

    /// <summary>Latência no percentil 50 (mediana) em milissegundos.</summary>
    public double LatencyP50Ms { get; init; }

    /// <summary>Latência no percentil 95 em milissegundos.</summary>
    public double LatencyP95Ms { get; init; }

    /// <summary>Latência no percentil 99 em milissegundos.</summary>
    public double LatencyP99Ms { get; init; }

    /// <summary>Latência máxima observada no intervalo em milissegundos.</summary>
    public double LatencyMaxMs { get; init; }

    // ── Recursos ──────────────────────────────────────────────────────

    /// <summary>Uso médio de CPU percentual no intervalo.</summary>
    public double? CpuAvgPercent { get; init; }

    /// <summary>Uso médio de memória em megabytes no intervalo.</summary>
    public double? MemoryAvgMb { get; init; }

    // ── Metadata ──────────────────────────────────────────────────────

    /// <summary>Timestamp de criação deste snapshot (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Snapshot de métricas agregadas de uma dependência entre serviços.
/// Registra throughput, latência e error rate na comunicação A → B.
///
/// Tabela-alvo: dependency_metrics_1m, dependency_metrics_1h.
/// Alimenta: topologia observada, blast radius, cost intelligence.
/// </summary>
public sealed record DependencyMetricsSnapshot
{
    /// <summary>Identificador único do snapshot.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Serviço de origem (caller).</summary>
    public required Guid SourceServiceId { get; init; }

    /// <summary>Nome do serviço de origem.</summary>
    public required string SourceServiceName { get; init; }

    /// <summary>Serviço de destino (callee).</summary>
    public required Guid TargetServiceId { get; init; }

    /// <summary>Nome do serviço de destino.</summary>
    public required string TargetServiceName { get; init; }

    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>Tenant ID para isolamento multi-tenant.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Nível de agregação.</summary>
    public required AggregationLevel AggregationLevel { get; init; }

    /// <summary>Início do intervalo de agregação.</summary>
    public required DateTimeOffset IntervalStart { get; init; }

    /// <summary>Fim do intervalo de agregação.</summary>
    public required DateTimeOffset IntervalEnd { get; init; }

    /// <summary>Total de chamadas no intervalo.</summary>
    public long CallCount { get; init; }

    /// <summary>Total de erros na comunicação.</summary>
    public long ErrorCount { get; init; }

    /// <summary>Taxa de erro percentual.</summary>
    public double ErrorRatePercent { get; init; }

    /// <summary>Latência média da dependência em milissegundos.</summary>
    public double LatencyAvgMs { get; init; }

    /// <summary>Latência P95 da dependência em milissegundos.</summary>
    public double LatencyP95Ms { get; init; }

    /// <summary>Latência P99 da dependência em milissegundos.</summary>
    public double LatencyP99Ms { get; init; }

    /// <summary>Timestamp de criação.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
