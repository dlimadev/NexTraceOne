using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Janela de observação pós-release para captura de indicadores after.
/// Permite comparação progressiva com o baseline (before/after).
/// Cada janela corresponde a uma fase da review automática.
/// </summary>
public sealed class ObservationWindow : AuditableEntity<ObservationWindowId>
{
    /// <summary>Release observada.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Fase de observação desta janela.</summary>
    public ObservationPhase Phase { get; private set; }

    /// <summary>Início da janela de observação (UTC).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Fim da janela de observação (UTC).</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Requisições por minuto observadas nesta janela.</summary>
    public decimal? RequestsPerMinute { get; private set; }

    /// <summary>Taxa de erro observada (0.0 a 1.0).</summary>
    public decimal? ErrorRate { get; private set; }

    /// <summary>Latência média observada em milissegundos.</summary>
    public decimal? AvgLatencyMs { get; private set; }

    /// <summary>Latência P95 observada em milissegundos.</summary>
    public decimal? P95LatencyMs { get; private set; }

    /// <summary>Latência P99 observada em milissegundos.</summary>
    public decimal? P99LatencyMs { get; private set; }

    /// <summary>Throughput observado (bytes/segundo).</summary>
    public decimal? Throughput { get; private set; }

    /// <summary>Indica se os dados desta janela já foram coletados.</summary>
    public bool IsCollected { get; private set; }

    /// <summary>Momento em que os dados foram coletados.</summary>
    public DateTimeOffset? CollectedAt { get; private set; }

    private ObservationWindow() { }

    /// <summary>
    /// Cria uma nova janela de observação para uma fase específica da review.
    /// </summary>
    public static ObservationWindow Create(
        ReleaseId releaseId,
        ObservationPhase phase,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        Guard.Against.Null(releaseId, nameof(releaseId));

        if (endsAt <= startsAt)
            throw new ArgumentException("Observation window end must be after start.");

        return new ObservationWindow
        {
            Id = ObservationWindowId.New(),
            ReleaseId = releaseId,
            Phase = phase,
            StartsAt = startsAt,
            EndsAt = endsAt,
            IsCollected = false
        };
    }

    /// <summary>
    /// Registra os indicadores observados nesta janela.
    /// </summary>
    public Result<Unit> RecordMetrics(
        decimal requestsPerMinute,
        decimal errorRate,
        decimal avgLatencyMs,
        decimal p95LatencyMs,
        decimal p99LatencyMs,
        decimal throughput,
        DateTimeOffset collectedAt)
    {
        if (IsCollected)
            return Error.Conflict(
                "change_intelligence.observation.already_collected",
                "Metrics have already been collected for this observation window.");

        RequestsPerMinute = requestsPerMinute;
        ErrorRate = errorRate;
        AvgLatencyMs = avgLatencyMs;
        P95LatencyMs = p95LatencyMs;
        P99LatencyMs = p99LatencyMs;
        Throughput = throughput;
        IsCollected = true;
        CollectedAt = collectedAt;

        return Result<Unit>.Success(Unit.Value);
    }
}

/// <summary>Identificador fortemente tipado para ObservationWindow.</summary>
public sealed record ObservationWindowId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static ObservationWindowId New() => new(Guid.NewGuid());
    /// <summary>Cria a partir de um Guid existente.</summary>
    public static ObservationWindowId From(Guid id) => new(id);
}
