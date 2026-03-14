using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RuntimeIntelligence.Domain.Errors;

namespace NexTraceOne.RuntimeIntelligence.Domain.Entities;

/// <summary>
/// Entidade que representa a baseline esperada de métricas de runtime de um serviço.
/// Serve como referência para detecção de drift — desvios entre o comportamento esperado
/// e o comportamento real observado em snapshots. A baseline é estabelecida a partir de
/// dados históricos e possui um score de confiança proporcional ao volume de dados analisados.
/// </summary>
public sealed class RuntimeBaseline : AuditableEntity<RuntimeBaselineId>
{
    private RuntimeBaseline() { }

    /// <summary>Nome do serviço ao qual esta baseline se refere.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente onde a baseline foi estabelecida (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Latência média esperada em milissegundos.</summary>
    public decimal ExpectedAvgLatencyMs { get; private set; }

    /// <summary>Latência P99 esperada em milissegundos.</summary>
    public decimal ExpectedP99LatencyMs { get; private set; }

    /// <summary>Taxa de erro esperada, expressa como fração entre 0 e 1.</summary>
    public decimal ExpectedErrorRate { get; private set; }

    /// <summary>Throughput esperado em requisições por segundo.</summary>
    public decimal ExpectedRequestsPerSecond { get; private set; }

    /// <summary>Data/hora UTC em que a baseline foi estabelecida.</summary>
    public DateTimeOffset EstablishedAt { get; private set; }

    /// <summary>Número de data points (snapshots) utilizados para calcular a baseline.</summary>
    public int DataPointCount { get; private set; }

    /// <summary>
    /// Score de confiança da baseline entre 0 e 1.
    /// Valores mais altos indicam maior volume de dados e maior estabilidade estatística.
    /// </summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>
    /// Estabelece uma nova baseline de runtime com validações de guarda nos campos obrigatórios.
    /// Métricas percentuais (ErrorRate, ConfidenceScore) são clamped ao intervalo válido.
    /// Requer pelo menos 1 data point para estabelecer a baseline.
    /// </summary>
    public static RuntimeBaseline Establish(
        string serviceName,
        string environment,
        decimal expectedAvgLatencyMs,
        decimal expectedP99LatencyMs,
        decimal expectedErrorRate,
        decimal expectedRequestsPerSecond,
        DateTimeOffset establishedAt,
        int dataPointCount,
        decimal confidenceScore)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.Negative(expectedAvgLatencyMs);
        Guard.Against.Negative(expectedP99LatencyMs);
        Guard.Against.Negative(expectedErrorRate);
        Guard.Against.Negative(expectedRequestsPerSecond);
        Guard.Against.NegativeOrZero(dataPointCount);
        Guard.Against.Negative(confidenceScore);

        return new RuntimeBaseline
        {
            Id = RuntimeBaselineId.New(),
            ServiceName = serviceName,
            Environment = environment,
            ExpectedAvgLatencyMs = expectedAvgLatencyMs,
            ExpectedP99LatencyMs = expectedP99LatencyMs,
            ExpectedErrorRate = Math.Clamp(expectedErrorRate, 0m, 1m),
            ExpectedRequestsPerSecond = expectedRequestsPerSecond,
            EstablishedAt = establishedAt,
            DataPointCount = dataPointCount,
            ConfidenceScore = Math.Clamp(confidenceScore, 0m, 1m)
        };
    }

    /// <summary>
    /// Verifica se um snapshot de runtime está dentro da tolerância definida em relação à baseline.
    /// Compara latência média, latência P99, taxa de erro e throughput.
    /// Retorna true se TODAS as métricas estiverem dentro da margem de tolerância percentual.
    /// </summary>
    /// <param name="snapshot">Snapshot de runtime a comparar com a baseline.</param>
    /// <param name="tolerancePercent">Percentual de tolerância (ex: 20 = ±20% de desvio permitido).</param>
    public bool IsWithinTolerance(RuntimeSnapshot snapshot, decimal tolerancePercent)
    {
        if (tolerancePercent <= 0m)
            return false;

        var factor = tolerancePercent / 100m;

        return IsMetricWithinTolerance(ExpectedAvgLatencyMs, snapshot.AvgLatencyMs, factor)
            && IsMetricWithinTolerance(ExpectedP99LatencyMs, snapshot.P99LatencyMs, factor)
            && IsMetricWithinTolerance(ExpectedErrorRate, snapshot.ErrorRate, factor)
            && IsMetricWithinTolerance(ExpectedRequestsPerSecond, snapshot.RequestsPerSecond, factor);
    }

    /// <summary>
    /// Verifica se o valor real de uma métrica está dentro da faixa de tolerância
    /// em relação ao valor esperado. Quando o valor esperado é zero, qualquer valor real
    /// dentro do factor absoluto é aceito para evitar divisão por zero.
    /// </summary>
    private static bool IsMetricWithinTolerance(decimal expected, decimal actual, decimal factor)
    {
        if (expected == 0m)
            return actual <= factor;

        var deviation = Math.Abs(actual - expected) / expected;
        return deviation <= factor;
    }
}

/// <summary>Identificador fortemente tipado de RuntimeBaseline.</summary>
public sealed record RuntimeBaselineId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RuntimeBaselineId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RuntimeBaselineId From(Guid id) => new(id);
}
