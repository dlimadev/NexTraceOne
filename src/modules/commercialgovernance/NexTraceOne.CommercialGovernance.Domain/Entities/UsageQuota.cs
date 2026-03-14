using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Licensing.Domain.Enums;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Quota de consumo associada a uma métrica licenciada.
/// Rastreia o uso acumulado contra o limite contratado e define
/// o nível de enforcement aplicável quando o limite é atingido.
///
/// Decisão de design:
/// - EnforcementLevel determina se o sistema alerta (Soft) ou bloqueia (Hard) ao exceder.
/// - WarningLevel é calculado dinamicamente com base no percentual de consumo.
/// - AlertThresholdPercentage mantém compatibilidade com o sistema de alertas existente.
/// - GracePeriodDays permite operação temporária após exceder o limite em modo Soft.
/// </summary>
public sealed class UsageQuota : Entity<UsageQuotaId>
{
    private UsageQuota() { }

    /// <summary>Código da métrica controlada pela quota.</summary>
    public string MetricCode { get; private set; } = string.Empty;

    /// <summary>Limite máximo permitido pela licença.</summary>
    public long Limit { get; private set; }

    /// <summary>Consumo acumulado atual.</summary>
    public long CurrentUsage { get; private set; }

    /// <summary>Percentual a partir do qual o alerta deve ser disparado.</summary>
    public decimal AlertThresholdPercentage { get; private set; }

    /// <summary>Nível de enforcement ao atingir o limite.</summary>
    public EnforcementLevel EnforcementLevel { get; private set; }

    /// <summary>Dias de tolerância após exceder o limite em modo Soft.</summary>
    public int GracePeriodDays { get; private set; }

    /// <summary>Data em que o overage foi detectado pela primeira vez (null se dentro do limite).</summary>
    public DateTimeOffset? OverageDetectedAt { get; private set; }

    /// <summary>Cria uma quota inicial para a métrica licenciada.</summary>
    public static UsageQuota Create(
        string metricCode,
        long limit,
        decimal alertThresholdPercentage = 0.80m,
        EnforcementLevel enforcementLevel = EnforcementLevel.Hard,
        int gracePeriodDays = 0)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        if (alertThresholdPercentage <= 0 || alertThresholdPercentage > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(alertThresholdPercentage), "Alert threshold must be between 0 and 1.");
        }

        if (gracePeriodDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gracePeriodDays), "Grace period must be zero or positive.");
        }

        return new UsageQuota
        {
            Id = UsageQuotaId.New(),
            MetricCode = Guard.Against.NullOrWhiteSpace(metricCode),
            Limit = limit,
            AlertThresholdPercentage = alertThresholdPercentage,
            EnforcementLevel = enforcementLevel,
            GracePeriodDays = gracePeriodDays
        };
    }

    /// <summary>Registra consumo adicional na quota, usando o instante fornecido para rastreio de overage.</summary>
    public void Consume(long quantity, DateTimeOffset now)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Consumed quantity must be greater than zero.");
        }

        CurrentUsage += quantity;

        if (IsExceeded() && OverageDetectedAt is null)
        {
            OverageDetectedAt = now;
        }
    }

    /// <summary>Indica se a quota foi excedida.</summary>
    public bool IsExceeded() => CurrentUsage > Limit;

    /// <summary>Indica se a quota alcançou o threshold de alerta configurado.</summary>
    public bool IsThresholdReached() => CurrentUsage >= Math.Ceiling(Limit * AlertThresholdPercentage);

    /// <summary>
    /// Calcula o nível de alerta progressivo com base no consumo atual.
    /// Thresholds: Normal(&lt;70%), Advisory(70%), Warning(85%), Critical(95%), Exceeded(100%).
    /// </summary>
    public WarningLevel GetWarningLevel()
    {
        if (Limit <= 0) return WarningLevel.Exceeded;

        var ratio = (decimal)CurrentUsage / Limit;
        return ratio switch
        {
            >= 1.0m => WarningLevel.Exceeded,
            >= 0.95m => WarningLevel.Critical,
            >= 0.85m => WarningLevel.Warning,
            >= 0.70m => WarningLevel.Advisory,
            _ => WarningLevel.Normal
        };
    }

    /// <summary>
    /// Verifica se o overage está dentro do grace period configurado.
    /// Retorna true se o consumo excedeu o limite mas ainda está em período de tolerância.
    /// </summary>
    public bool IsInGracePeriod(DateTimeOffset now)
    {
        if (!IsExceeded() || OverageDetectedAt is null || GracePeriodDays <= 0)
            return false;

        return now <= OverageDetectedAt.Value.AddDays(GracePeriodDays);
    }

    /// <summary>
    /// Determina se a operação deve ser bloqueada com base no enforcement level,
    /// consumo atual e grace period.
    /// </summary>
    public bool ShouldBlock(DateTimeOffset now)
    {
        if (!IsExceeded()) return false;

        return EnforcementLevel switch
        {
            EnforcementLevel.Soft => !IsInGracePeriod(now),
            EnforcementLevel.Hard => true,
            EnforcementLevel.NeverBreak => false,
            _ => true
        };
    }

    /// <summary>Percentual de consumo atual em relação ao limite (0.0 a N).</summary>
    public decimal UsagePercentage => Limit > 0 ? (decimal)CurrentUsage / Limit : 1.0m;
}

/// <summary>Identificador fortemente tipado de UsageQuota.</summary>
public sealed record UsageQuotaId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static UsageQuotaId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static UsageQuotaId From(Guid id) => new(id);
}
