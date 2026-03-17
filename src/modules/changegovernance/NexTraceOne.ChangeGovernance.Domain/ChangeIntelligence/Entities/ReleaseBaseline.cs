using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Baseline de indicadores pré-release capturada antes do deploy.
/// Serve como referência para comparação before/after e para a
/// review automática pós-release. Os indicadores são agregados
/// oriundos do Telemetry Store (OpenTelemetry), nunca dados crus.
/// </summary>
public sealed class ReleaseBaseline : AuditableEntity<ReleaseBaselineId>
{
    /// <summary>Release associada ao baseline.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Requisições por minuto médias no período baseline.</summary>
    public decimal RequestsPerMinute { get; private set; }

    /// <summary>Taxa de erro média no período baseline (0.0 a 1.0).</summary>
    public decimal ErrorRate { get; private set; }

    /// <summary>Latência média em milissegundos.</summary>
    public decimal AvgLatencyMs { get; private set; }

    /// <summary>Latência P95 em milissegundos.</summary>
    public decimal P95LatencyMs { get; private set; }

    /// <summary>Latência P99 em milissegundos.</summary>
    public decimal P99LatencyMs { get; private set; }

    /// <summary>Throughput médio (bytes/segundo).</summary>
    public decimal Throughput { get; private set; }

    /// <summary>Início do período de coleta do baseline.</summary>
    public DateTimeOffset CollectedFrom { get; private set; }

    /// <summary>Fim do período de coleta do baseline.</summary>
    public DateTimeOffset CollectedTo { get; private set; }

    /// <summary>Momento de captura do baseline.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    private ReleaseBaseline() { }

    /// <summary>
    /// Registra um baseline de indicadores para a release especificada.
    /// </summary>
    public static ReleaseBaseline Create(
        ReleaseId releaseId,
        decimal requestsPerMinute,
        decimal errorRate,
        decimal avgLatencyMs,
        decimal p95LatencyMs,
        decimal p99LatencyMs,
        decimal throughput,
        DateTimeOffset collectedFrom,
        DateTimeOffset collectedTo,
        DateTimeOffset capturedAt)
    {
        Guard.Against.Null(releaseId, nameof(releaseId));

        return new ReleaseBaseline
        {
            Id = ReleaseBaselineId.New(),
            ReleaseId = releaseId,
            RequestsPerMinute = requestsPerMinute,
            ErrorRate = errorRate,
            AvgLatencyMs = avgLatencyMs,
            P95LatencyMs = p95LatencyMs,
            P99LatencyMs = p99LatencyMs,
            Throughput = throughput,
            CollectedFrom = collectedFrom,
            CollectedTo = collectedTo,
            CapturedAt = capturedAt
        };
    }
}

/// <summary>Identificador fortemente tipado para ReleaseBaseline.</summary>
public sealed record ReleaseBaselineId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static ReleaseBaselineId New() => new(Guid.NewGuid());
    /// <summary>Cria a partir de um Guid existente.</summary>
    public static ReleaseBaselineId From(Guid id) => new(id);
}
