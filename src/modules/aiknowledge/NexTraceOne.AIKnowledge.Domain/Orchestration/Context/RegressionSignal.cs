using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Context;

/// <summary>
/// Value Object que representa um sinal de regressão detectado pela IA
/// ao comparar o comportamento atual de um ambiente com um baseline esperado.
///
/// Sinais de regressão são evidências específicas e mensuráveis de degradação.
/// Diferente de RiskFinding (achado de risco), um RegressionSignal é uma medição:
/// tem valor atual, valor baseline e delta calculado.
///
/// Exemplos:
/// - Latência média aumentou 40% em relação ao baseline de produção
/// - Taxa de erro do serviço X é 3x maior que o esperado em staging
/// - Throughput caiu 25% comparado com a semana anterior
/// </summary>
public sealed class RegressionSignal : ValueObject
{
    /// <summary>Nome do serviço onde o sinal foi detectado.</summary>
    public string ServiceName { get; }

    /// <summary>Métrica que apresentou regressão.</summary>
    public string MetricName { get; }

    /// <summary>Valor atual da métrica no ambiente sendo avaliado.</summary>
    public double CurrentValue { get; }

    /// <summary>Valor baseline esperado (do ambiente de referência).</summary>
    public double BaselineValue { get; }

    /// <summary>Delta percentual entre o valor atual e o baseline.</summary>
    public double DeltaPercent { get; }

    /// <summary>Unidade da métrica (ex.: "ms", "%", "req/s", "count").</summary>
    public string Unit { get; }

    /// <summary>Indica se o delta representa deterioração (true) ou melhoria (false).</summary>
    public bool IsDegradation { get; }

    /// <summary>Intensidade da regressão.</summary>
    public RegressionIntensity Intensity { get; }

    /// <summary>Data/hora UTC em que o sinal foi detectado.</summary>
    public DateTimeOffset DetectedAt { get; }

    private RegressionSignal(
        string serviceName,
        string metricName,
        double currentValue,
        double baselineValue,
        double deltaPercent,
        string unit,
        bool isDegradation,
        RegressionIntensity intensity,
        DateTimeOffset detectedAt)
    {
        ServiceName = serviceName;
        MetricName = metricName;
        CurrentValue = currentValue;
        BaselineValue = baselineValue;
        DeltaPercent = deltaPercent;
        Unit = unit;
        IsDegradation = isDegradation;
        Intensity = intensity;
        DetectedAt = detectedAt;
    }

    /// <summary>
    /// Cria um sinal de regressão a partir de valores observados e esperados.
    /// A intensidade é calculada automaticamente com base no delta percentual absoluto.
    /// </summary>
    public static RegressionSignal Create(
        string serviceName,
        string metricName,
        double currentValue,
        double baselineValue,
        string unit,
        DateTimeOffset detectedAt,
        bool higherIsBetter = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);

        var delta = baselineValue != 0
            ? ((currentValue - baselineValue) / Math.Abs(baselineValue)) * 100.0
            : 0;

        // Deterioração: para métricas onde menor é melhor (latência, erros), aumento é ruim.
        // Para métricas onde maior é melhor (throughput, availability), diminuição é ruim.
        var isDegradation = higherIsBetter ? currentValue < baselineValue : currentValue > baselineValue;

        var absDelta = Math.Abs(delta);
        var intensity = absDelta switch
        {
            < IntensityThresholds.Minor => RegressionIntensity.Negligible,
            < IntensityThresholds.Moderate => RegressionIntensity.Minor,
            < IntensityThresholds.Significant => RegressionIntensity.Moderate,
            < IntensityThresholds.Severe => RegressionIntensity.Significant,
            _ => RegressionIntensity.Severe
        };

        return new RegressionSignal(
            serviceName, metricName, currentValue, baselineValue,
            delta, unit, isDegradation, intensity, detectedAt);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ServiceName;
        yield return MetricName;
        yield return DetectedAt;
    }
}

/// <summary>Intensidade de um sinal de regressão.</summary>
public enum RegressionIntensity
{
    /// <summary>Delta < 5%. Dentro da margem de ruído normal.</summary>
    Negligible = 1,

    /// <summary>Delta 5-15%. Variação leve, monitorar.</summary>
    Minor = 2,

    /// <summary>Delta 15-30%. Variação moderada, investigar.</summary>
    Moderate = 3,

    /// <summary>Delta 30-50%. Regressão significativa, agir.</summary>
    Significant = 4,

    /// <summary>Delta > 50%. Regressão severa, bloquear promoção.</summary>
    Severe = 5
}

/// <summary>
/// Limiares percentuais para classificação de intensidade de regressão.
/// Extraídos para facilitar ajuste fino sem alteração de lógica.
/// </summary>
public static class IntensityThresholds
{
    public const double Minor = 5.0;
    public const double Moderate = 15.0;
    public const double Significant = 30.0;
    public const double Severe = 50.0;
}
