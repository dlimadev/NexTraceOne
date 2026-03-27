using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Snapshot persistido do burn rate do error budget de um SLO numa janela de tempo.
/// Representa a velocidade de consumo do budget — quantas vezes mais rápido o budget
/// está a ser consumido em relação ao ritmo esperado para que o SLO seja cumprido.
///
/// Um burn rate de 1.0 indica consumo sustentável (o budget dura exatamente o período completo).
/// Um burn rate &gt; 1.0 indica consumo acelerado — quanto maior, mais crítico.
/// </summary>
public sealed class BurnRateSnapshot : AuditableEntity<BurnRateSnapshotId>
{
    private BurnRateSnapshot() { }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Referência ao SLO que originou este cálculo.</summary>
    public SloDefinitionId SloDefinitionId { get; private set; } = null!;

    /// <summary>Identificador do serviço (desnormalizado para queries diretas).</summary>
    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>Ambiente (desnormalizado para queries diretas).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>
    /// Janela de tempo sobre a qual o burn rate foi calculado.
    /// Janelas curtas detetam picos; janelas longas mostram tendência.
    /// </summary>
    public BurnRateWindow Window { get; private set; }

    /// <summary>
    /// Valor do burn rate calculado.
    /// 1.0 = consumo exatamente sustentável; &gt; 1.0 = acelerado; &lt; 1.0 = abaixo do esperado.
    /// </summary>
    public decimal BurnRate { get; private set; }

    /// <summary>Taxa de erros observada na janela (0.0 – 1.0).</summary>
    public decimal ObservedErrorRate { get; private set; }

    /// <summary>Taxa de erros tolerada pelo SLO (derivada de TargetPercent).</summary>
    public decimal ToleratedErrorRate { get; private set; }

    /// <summary>Estado do SLO inferido a partir do burn rate.</summary>
    public SloStatus Status { get; private set; }

    /// <summary>Instante em que o snapshot foi calculado.</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Navegação para o SLO base.</summary>
    public SloDefinition SloDefinition { get; private set; } = null!;

    /// <summary>
    /// Cria um novo snapshot de burn rate com os valores calculados.
    /// </summary>
    public static BurnRateSnapshot Create(
        Guid tenantId,
        SloDefinitionId sloDefinitionId,
        string serviceId,
        string environment,
        BurnRateWindow window,
        decimal observedErrorRate,
        decimal toleratedErrorRate,
        DateTimeOffset computedAt)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.Null(sloDefinitionId);
        Guard.Against.NullOrWhiteSpace(serviceId);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.OutOfRange(observedErrorRate, nameof(observedErrorRate), 0m, 1m);
        Guard.Against.OutOfRange(toleratedErrorRate, nameof(toleratedErrorRate), 0m, 1m);

        var burnRate = toleratedErrorRate > 0m
            ? Math.Round(observedErrorRate / toleratedErrorRate, 4)
            : observedErrorRate > 0m ? 999m : 0m;

        var status = burnRate >= 14.4m
            ? SloStatus.Violated    // alertas de burn rate crítico (Google SRE heurística)
            : burnRate >= 6m
                ? SloStatus.AtRisk
                : SloStatus.Healthy;

        return new BurnRateSnapshot
        {
            Id = BurnRateSnapshotId.New(),
            TenantId = tenantId,
            SloDefinitionId = sloDefinitionId,
            ServiceId = serviceId,
            Environment = environment,
            Window = window,
            BurnRate = burnRate,
            ObservedErrorRate = observedErrorRate,
            ToleratedErrorRate = toleratedErrorRate,
            Status = status,
            ComputedAt = computedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de BurnRateSnapshot.</summary>
public sealed record BurnRateSnapshotId(Guid Value) : TypedIdBase(Value)
{
    public static BurnRateSnapshotId New() => new(Guid.NewGuid());
    public static BurnRateSnapshotId From(Guid id) => new(id);
}
