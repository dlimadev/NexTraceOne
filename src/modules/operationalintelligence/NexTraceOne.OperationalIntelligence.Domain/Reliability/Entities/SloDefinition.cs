using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Definição persistida de um SLO (Service Level Objective) para um serviço num ambiente.
/// Representa o acordo interno sobre o nível de serviço esperado: tipo, objetivo numérico,
/// janela de medição e ambiente de aplicação.
///
/// Um SLO é o alicerce do error budget e do burn rate — sem uma definição de SLO persistida,
/// não é possível calcular nem auditar o consumo de budget de forma determinística.
/// </summary>
public sealed class SloDefinition : AuditableEntity<SloDefinitionId>
{
    private SloDefinition() { }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Identificador do serviço ao qual o SLO se aplica.</summary>
    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>Ambiente ao qual o SLO se aplica (ex: production, staging).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Nome descritivo do SLO (ex: "API availability – production").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição opcional do objetivo.</summary>
    public string? Description { get; private set; }

    /// <summary>Tipo de indicador medido pelo SLO.</summary>
    public SloType Type { get; private set; }

    /// <summary>
    /// Objetivo numérico do SLO expresso em percentagem (0–100).
    /// Exemplos: 99.9 para disponibilidade; 5.0 para taxa de erro máxima.
    /// </summary>
    public decimal TargetPercent { get; private set; }

    /// <summary>
    /// Limiar de alerta (percentagem) — abaixo deste valor o SLO entra em estado AtRisk.
    /// Se nulo, assume TargetPercent menos 0.5 como heurística.
    /// </summary>
    public decimal? AlertThresholdPercent { get; private set; }

    /// <summary>Janela de medição em dias (ex: 30 para rolling 30 dias).</summary>
    public int WindowDays { get; private set; }

    /// <summary>Indica se o SLO está ativo e a ser monitorizado.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova definição de SLO com os parâmetros fornecidos.
    /// </summary>
    public static SloDefinition Create(
        Guid tenantId,
        string serviceId,
        string environment,
        string name,
        SloType type,
        decimal targetPercent,
        int windowDays,
        string? description = null,
        decimal? alertThresholdPercent = null)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.NullOrWhiteSpace(serviceId);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(targetPercent, nameof(targetPercent), 0m, 100m);
        Guard.Against.NegativeOrZero(windowDays);

        if (alertThresholdPercent.HasValue)
            Guard.Against.OutOfRange(alertThresholdPercent.Value, nameof(alertThresholdPercent), 0m, 100m);

        return new SloDefinition
        {
            Id = SloDefinitionId.New(),
            TenantId = tenantId,
            ServiceId = serviceId,
            Environment = environment,
            Name = name,
            Type = type,
            TargetPercent = targetPercent,
            AlertThresholdPercent = alertThresholdPercent,
            WindowDays = windowDays,
            Description = description,
            IsActive = true
        };
    }

    /// <summary>Desativa o SLO, impedindo novos cálculos de budget a partir deste objetivo.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Atualiza o objetivo numérico e o limiar de alerta.</summary>
    public void UpdateTarget(decimal targetPercent, decimal? alertThresholdPercent = null)
    {
        Guard.Against.OutOfRange(targetPercent, nameof(targetPercent), 0m, 100m);
        if (alertThresholdPercent.HasValue)
            Guard.Against.OutOfRange(alertThresholdPercent.Value, nameof(alertThresholdPercent), 0m, 100m);

        TargetPercent = targetPercent;
        AlertThresholdPercent = alertThresholdPercent;
    }
}

/// <summary>Identificador fortemente tipado de SloDefinition.</summary>
public sealed record SloDefinitionId(Guid Value) : TypedIdBase(Value)
{
    public static SloDefinitionId New() => new(Guid.NewGuid());
    public static SloDefinitionId From(Guid id) => new(id);
}
