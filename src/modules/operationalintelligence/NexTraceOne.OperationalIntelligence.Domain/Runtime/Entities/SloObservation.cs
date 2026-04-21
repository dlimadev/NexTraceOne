using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Aggregate Root que representa uma observação pontual de um SLO (Service Level Objective).
///
/// Cada registo captura o valor medido de uma métrica de serviço num período,
/// comparando-o com o objetivo definido (SloTarget) e classificando o estado resultante.
///
/// Usado por GetSloComplianceSummary (agregação de conformidade) e
/// GetSloViolationTrend (tendência histórica de violações).
///
/// Wave J.2 — SLO Tracking (OperationalIntelligence).
/// </summary>
public sealed class SloObservation : AuditableEntity<SloObservationId>
{
    private const int MaxServiceNameLength = 200;
    private const int MaxEnvironmentLength = 100;
    private const int MaxTenantIdLength = 100;
    private const int MaxMetricNameLength = 200;
    private const int MaxUnitLength = 50;

    private SloObservation() { }

    /// <summary>Identificador do tenant ao qual a observação pertence.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Nome do serviço avaliado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente de operação (e.g. production, staging).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Nome da métrica observada (e.g. "availability", "latency_p99", "error_rate").</summary>
    public string MetricName { get; private set; } = string.Empty;

    /// <summary>Valor observado da métrica no período.</summary>
    public decimal ObservedValue { get; private set; }

    /// <summary>Valor objetivo do SLO (e.g. 99.9 para 99.9% de disponibilidade).</summary>
    public decimal SloTarget { get; private set; }

    /// <summary>Unidade da métrica (e.g. "percent", "ms", "rpm").</summary>
    public string Unit { get; private set; } = string.Empty;

    /// <summary>Estado desta observação face ao objetivo de SLO.</summary>
    public SloObservationStatus Status { get; private set; }

    /// <summary>Início do período de observação (UTC).</summary>
    public DateTimeOffset PeriodStart { get; private set; }

    /// <summary>Fim do período de observação (UTC).</summary>
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>Momento em que a observação foi registada (UTC).</summary>
    public DateTimeOffset ObservedAt { get; private set; }

    /// <summary>
    /// Cria e classifica uma nova observação de SLO.
    /// O estado é calculado automaticamente com base no valor observado vs objetivo.
    /// </summary>
    public static SloObservation Create(
        string tenantId,
        string serviceName,
        string environment,
        string metricName,
        decimal observedValue,
        decimal sloTarget,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset observedAt,
        string? unit = null)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(metricName);
        Guard.Against.Negative(sloTarget);
        Guard.Against.InvalidInput(periodEnd, nameof(periodEnd), v => v > periodStart,
            "PeriodEnd must be after PeriodStart.");

        var status = ClassifyStatus(observedValue, sloTarget);

        return new SloObservation
        {
            Id = SloObservationId.New(),
            TenantId = tenantId[..Math.Min(tenantId.Length, MaxTenantIdLength)],
            ServiceName = serviceName[..Math.Min(serviceName.Length, MaxServiceNameLength)],
            Environment = environment[..Math.Min(environment.Length, MaxEnvironmentLength)],
            MetricName = metricName[..Math.Min(metricName.Length, MaxMetricNameLength)],
            ObservedValue = observedValue,
            SloTarget = sloTarget,
            Unit = (unit ?? "")[..Math.Min((unit ?? "").Length, MaxUnitLength)],
            Status = status,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            ObservedAt = observedAt,
        };
    }

    /// <summary>
    /// Classifica o estado da observação:
    /// - Met: valor atinge ou supera o objetivo
    /// - Warning: valor está dentro de 10% abaixo do objetivo
    /// - Breached: valor está abaixo do limiar de alerta
    /// </summary>
    private static SloObservationStatus ClassifyStatus(decimal observed, decimal target)
    {
        if (target <= 0) return SloObservationStatus.Met;
        if (observed >= target) return SloObservationStatus.Met;
        var gap = (target - observed) / target;
        return gap <= 0.10m ? SloObservationStatus.Warning : SloObservationStatus.Breached;
    }
}

/// <summary>Strongly-typed ID para SloObservation.</summary>
public sealed record SloObservationId(Guid Value) : TypedIdBase(Value)
{
    public static SloObservationId New() => new(Guid.NewGuid());
    public static SloObservationId From(Guid id) => new(id);
}
