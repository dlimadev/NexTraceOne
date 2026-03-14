namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

/// <summary>
/// Snapshot de anomalia detectada pelo pipeline de telemetria.
/// Registra desvios significativos de baseline para investigação e correlação.
///
/// Tabela-alvo: anomaly_snapshots (Product Store — PostgreSQL).
/// Alimenta: alertas, correlation com releases, investigation context, AI orchestration.
/// </summary>
public sealed record AnomalySnapshot
{
    /// <summary>Identificador único da anomalia.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Identificador do serviço afetado.</summary>
    public required Guid ServiceId { get; init; }

    /// <summary>Nome do serviço afetado.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Ambiente em que a anomalia foi detectada.</summary>
    public required string Environment { get; init; }

    /// <summary>Tenant ID para isolamento multi-tenant.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Tipo de anomalia detectada.
    /// Exemplos: "latency_spike", "error_rate_surge", "throughput_drop",
    /// "cpu_spike", "memory_leak", "dependency_degradation".
    /// </summary>
    public required string AnomalyType { get; init; }

    /// <summary>
    /// Severidade da anomalia (1 = informacional, 2 = warning, 3 = critical).
    /// </summary>
    public required int Severity { get; init; }

    /// <summary>Descrição técnica da anomalia em inglês (para logs e investigação).</summary>
    public required string Description { get; init; }

    /// <summary>Chave i18n para exibição no frontend.</summary>
    public required string MessageKey { get; init; }

    /// <summary>Valor observado que disparou a anomalia.</summary>
    public double ObservedValue { get; init; }

    /// <summary>Valor esperado (baseline) para comparação.</summary>
    public double ExpectedValue { get; init; }

    /// <summary>Desvio percentual em relação ao baseline.</summary>
    public double DeviationPercent { get; init; }

    /// <summary>Início da janela de detecção da anomalia.</summary>
    public required DateTimeOffset DetectedAt { get; init; }

    /// <summary>Fim da anomalia (quando voltou ao baseline). Null se ainda ativa.</summary>
    public DateTimeOffset? ResolvedAt { get; init; }

    /// <summary>
    /// ID da release correlacionada (se detectada via release_runtime_correlation).
    /// Permite rastrear se a anomalia foi causada por um deploy específico.
    /// </summary>
    public Guid? CorrelatedReleaseId { get; init; }

    /// <summary>Timestamp de criação do snapshot.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
