using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CostIntelligence.Domain.Enums;
using NexTraceOne.CostIntelligence.Domain.Errors;

namespace NexTraceOne.CostIntelligence.Domain.Entities;

/// <summary>
/// Entidade que representa a análise de tendência de custo de um serviço ao longo de um período.
/// Agrega dados estatísticos (média, pico, variação percentual) e classifica a direção
/// da tendência para alertar sobre aumentos ou reduções significativas.
/// </summary>
public sealed class CostTrend : AuditableEntity<CostTrendId>
{
    /// <summary>
    /// Limiar de variação percentual para considerar a tendência como significativa.
    /// Variações dentro de ±5% são classificadas como estáveis.
    /// </summary>
    private const decimal StableThresholdPercent = 5m;

    private CostTrend() { }

    /// <summary>Nome do serviço analisado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente analisado (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Início do período de análise de tendência.</summary>
    public DateTimeOffset PeriodStart { get; private set; }

    /// <summary>Fim do período de análise de tendência.</summary>
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>Custo médio diário no período analisado.</summary>
    public decimal AverageDailyCost { get; private set; }

    /// <summary>Maior custo diário registrado no período.</summary>
    public decimal PeakDailyCost { get; private set; }

    /// <summary>Direção da tendência de custo: Rising, Stable ou Declining.</summary>
    public TrendDirection TrendDirection { get; private set; } = TrendDirection.Stable;

    /// <summary>Variação percentual do custo no período (positiva = aumento, negativa = redução).</summary>
    public decimal PercentageChange { get; private set; }

    /// <summary>Número de pontos de dados (snapshots) utilizados na análise.</summary>
    public int DataPointCount { get; private set; }

    /// <summary>
    /// Indica se a tendência representa uma variação significativa de custo.
    /// Tendências Rising ou Declining são consideradas significativas — podem exigir ação.
    /// </summary>
    public bool IsSignificant => TrendDirection != TrendDirection.Stable;

    /// <summary>
    /// Indica se a tendência representa aumento de custo acima do limiar de estabilidade.
    /// </summary>
    public bool IsRising => TrendDirection == TrendDirection.Rising;

    /// <summary>
    /// Indica se a tendência representa redução de custo abaixo do limiar de estabilidade.
    /// </summary>
    public bool IsDeclining => TrendDirection == TrendDirection.Declining;

    /// <summary>
    /// Cria uma nova análise de tendência de custo para um serviço e período específicos.
    /// Valida consistência do período e valores numéricos.
    /// A classificação da direção é encapsulada — feita automaticamente pelo factory.
    /// </summary>
    public static Result<CostTrend> Create(
        string serviceName,
        string environment,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        decimal averageDailyCost,
        decimal peakDailyCost,
        decimal percentageChange,
        int dataPointCount)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.Negative(averageDailyCost);
        Guard.Against.Negative(peakDailyCost);
        Guard.Against.NegativeOrZero(dataPointCount);

        if (periodStart >= periodEnd)
            return CostIntelligenceErrors.InvalidPeriod(periodStart, periodEnd);

        var trend = new CostTrend
        {
            Id = CostTrendId.New(),
            ServiceName = serviceName,
            Environment = environment,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AverageDailyCost = averageDailyCost,
            PeakDailyCost = peakDailyCost,
            PercentageChange = percentageChange,
            DataPointCount = dataPointCount
        };

        trend.DeriveDirection();

        return trend;
    }

    /// <summary>
    /// Classifica a direção da tendência com base na variação percentual.
    /// Encapsulado: chamado apenas pelo factory method para garantir invariante.
    /// </summary>
    private void DeriveDirection()
    {
        TrendDirection = PercentageChange switch
        {
            > StableThresholdPercent => TrendDirection.Rising,
            < -StableThresholdPercent => TrendDirection.Declining,
            _ => TrendDirection.Stable
        };
    }
}

/// <summary>Identificador fortemente tipado de CostTrend.</summary>
public sealed record CostTrendId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CostTrendId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CostTrendId From(Guid id) => new(id);
}
