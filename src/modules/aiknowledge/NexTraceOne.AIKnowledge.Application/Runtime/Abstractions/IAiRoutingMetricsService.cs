namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de métricas de roteamento de IA.
/// Rastreia latência, taxa de sucesso e custo por provider/modelo
/// para suportar roteamento inteligente (latency-aware e cost-aware).
/// </summary>
public interface IAiRoutingMetricsService
{
    /// <summary>
    /// Registra uma execução de inferência para métricas.
    /// </summary>
    Task RecordExecutionAsync(
        string providerId,
        string modelId,
        TimeSpan duration,
        int promptTokens,
        int completionTokens,
        bool success,
        CancellationToken ct = default);

    /// <summary>
    /// Obtém métricas agregadas para um provider/modelo específico.
    /// </summary>
    Task<RoutingMetricsSnapshot?> GetMetricsAsync(
        string providerId,
        string modelId,
        CancellationToken ct = default);

    /// <summary>
    /// Ranqueia providers por latência média (menor primeiro).
    /// Considera apenas providers com dados históricos suficientes.
    /// </summary>
    Task<IReadOnlyList<ProviderLatencyRanking>> RankProvidersByLatencyAsync(
        IEnumerable<string> providerIds,
        CancellationToken ct = default);

    /// <summary>
    /// Ranqueia providers por custo médio por token (menor primeiro).
    /// </summary>
    Task<IReadOnlyList<ProviderCostRanking>> RankProvidersByCostAsync(
        IEnumerable<string> providerIds,
        CancellationToken ct = default);
}

/// <summary>Snapshot de métricas de roteamento para um provider/modelo.</summary>
public sealed record RoutingMetricsSnapshot(
    string ProviderId,
    string ModelId,
    double AverageLatencyMs,
    double MedianLatencyMs,
    long TotalExecutions,
    long FailedExecutions,
    double SuccessRate,
    decimal? AverageCostPer1KTokens);

/// <summary>Ranking de provider por latência.</summary>
public sealed record ProviderLatencyRanking(
    string ProviderId,
    double AverageLatencyMs,
    long ExecutionCount,
    double SuccessRate);

/// <summary>Ranking de provider por custo.</summary>
public sealed record ProviderCostRanking(
    string ProviderId,
    decimal AverageCostPer1KTokens,
    long ExecutionCount);
