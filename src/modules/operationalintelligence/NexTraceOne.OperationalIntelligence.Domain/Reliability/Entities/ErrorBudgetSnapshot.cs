using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Snapshot persistido do estado do error budget de um SLO num ponto no tempo.
/// Regista a quantidade de budget disponível e consumido, permitindo trending histórico
/// e decisões de promoção de mudanças baseadas em disponibilidade de budget.
///
/// O error budget é calculado a partir da diferença entre o objetivo do SLO e o
/// comportamento real observado na janela de medição configurada.
/// </summary>
public sealed class ErrorBudgetSnapshot : AuditableEntity<ErrorBudgetSnapshotId>
{
    private ErrorBudgetSnapshot() { }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Referência ao SLO que originou este cálculo de budget.</summary>
    public SloDefinitionId SloDefinitionId { get; private set; } = null!;

    /// <summary>Identificador do serviço (desnormalizado para queries diretas).</summary>
    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>Ambiente (desnormalizado para queries diretas).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>
    /// Budget total disponível no período, em minutos ou unidades de falha,
    /// conforme o tipo de SLO. Calculado como (1 - target) × window.
    /// </summary>
    public decimal TotalBudgetMinutes { get; private set; }

    /// <summary>Budget consumido até ao momento do snapshot.</summary>
    public decimal ConsumedBudgetMinutes { get; private set; }

    /// <summary>Budget remanescente = Total – Consumed.</summary>
    public decimal RemainingBudgetMinutes { get; private set; }

    /// <summary>Percentagem do budget consumido (0–100).</summary>
    public decimal ConsumedPercent { get; private set; }

    /// <summary>Estado do SLO derivado do consumo actual do budget.</summary>
    public SloStatus Status { get; private set; }

    /// <summary>Instante em que o snapshot foi calculado.</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Navegação para o SLO base.</summary>
    public SloDefinition SloDefinition { get; private set; } = null!;

    /// <summary>
    /// Cria um novo snapshot de error budget com os valores calculados.
    /// </summary>
    public static ErrorBudgetSnapshot Create(
        Guid tenantId,
        SloDefinitionId sloDefinitionId,
        string serviceId,
        string environment,
        decimal totalBudgetMinutes,
        decimal consumedBudgetMinutes,
        DateTimeOffset computedAt)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.Null(sloDefinitionId);
        Guard.Against.NullOrWhiteSpace(serviceId);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.Negative(totalBudgetMinutes);
        Guard.Against.Negative(consumedBudgetMinutes);

        var remaining = Math.Max(0m, totalBudgetMinutes - consumedBudgetMinutes);
        var consumedPct = totalBudgetMinutes > 0m
            ? Math.Round(Math.Min(100m, (consumedBudgetMinutes / totalBudgetMinutes) * 100m), 4)
            : 100m;

        var status = consumedPct >= 100m
            ? SloStatus.Violated
            : consumedPct >= 80m
                ? SloStatus.AtRisk
                : SloStatus.Healthy;

        return new ErrorBudgetSnapshot
        {
            Id = ErrorBudgetSnapshotId.New(),
            TenantId = tenantId,
            SloDefinitionId = sloDefinitionId,
            ServiceId = serviceId,
            Environment = environment,
            TotalBudgetMinutes = totalBudgetMinutes,
            ConsumedBudgetMinutes = consumedBudgetMinutes,
            RemainingBudgetMinutes = remaining,
            ConsumedPercent = consumedPct,
            Status = status,
            ComputedAt = computedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ErrorBudgetSnapshot.</summary>
public sealed record ErrorBudgetSnapshotId(Guid Value) : TypedIdBase(Value)
{
    public static ErrorBudgetSnapshotId New() => new(Guid.NewGuid());
    public static ErrorBudgetSnapshotId From(Guid id) => new(id);
}
