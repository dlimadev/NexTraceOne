using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Quota de consumo associada a uma métrica licenciada.
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

    /// <summary>Cria uma quota inicial para a métrica licenciada.</summary>
    public static UsageQuota Create(string metricCode, long limit, decimal alertThresholdPercentage = 0.80m)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        if (alertThresholdPercentage <= 0 || alertThresholdPercentage > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(alertThresholdPercentage), "Alert threshold must be between 0 and 1.");
        }

        return new UsageQuota
        {
            Id = UsageQuotaId.New(),
            MetricCode = Guard.Against.NullOrWhiteSpace(metricCode),
            Limit = limit,
            AlertThresholdPercentage = alertThresholdPercentage
        };
    }

    /// <summary>Registra consumo adicional na quota.</summary>
    public void Consume(long quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Consumed quantity must be greater than zero.");
        }

        CurrentUsage += quantity;
    }

    /// <summary>Indica se a quota foi excedida.</summary>
    public bool IsExceeded() => CurrentUsage > Limit;

    /// <summary>Indica se a quota alcançou o threshold de alerta configurado.</summary>
    public bool IsThresholdReached() => CurrentUsage >= Math.Ceiling(Limit * AlertThresholdPercentage);
}

/// <summary>Identificador fortemente tipado de UsageQuota.</summary>
public sealed record UsageQuotaId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static UsageQuotaId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static UsageQuotaId From(Guid id) => new(id);
}
