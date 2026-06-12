using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação de métricas de roteamento usando o ledger de uso de tokens como fonte de dados.
/// Calcula latência média, taxa de sucesso e custo por provider a partir de execuções históricas.
/// </summary>
public sealed class AiRoutingMetricsService : IAiRoutingMetricsService
{
    private readonly IAiTokenUsageLedgerRepository _ledgerRepository;
    private readonly ILogger<AiRoutingMetricsService> _logger;

    // Janela de lookback para métricas (7 dias)
    private static readonly TimeSpan MetricsLookbackWindow = TimeSpan.FromDays(7);

    // Mínimo de execuções para considerar métricas confiáveis
    private const int MinExecutionThreshold = 3;

    public AiRoutingMetricsService(
        IAiTokenUsageLedgerRepository ledgerRepository,
        ILogger<AiRoutingMetricsService> logger)
    {
        _ledgerRepository = ledgerRepository;
        _logger = logger;
    }

    public Task RecordExecutionAsync(
        string providerId,
        string modelId,
        TimeSpan duration,
        int promptTokens,
        int completionTokens,
        bool success,
        CancellationToken ct = default)
    {
        // O registro é feito pelo TokenQuotaService/AiExecutionGateway via ledger.
        // Este método é um no-op porque os dados já fluem para o repositório.
        // Futuramente pode usar um cache in-memory para métricas em tempo real.
        return Task.CompletedTask;
    }

    public async Task<RoutingMetricsSnapshot?> GetMetricsAsync(
        string providerId,
        string modelId,
        CancellationToken ct = default)
    {
        var entries = await GetRecentEntriesAsync(ct);

        var relevant = entries
            .Where(e =>
                string.Equals(e.ProviderId, providerId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.ModelId, modelId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (relevant.Count == 0)
            return null;

        var latencies = relevant.Select(e => e.DurationMs).OrderBy(d => d).ToList();
        var avgLatency = latencies.Average();
        var medianLatency = latencies.Count % 2 == 1
            ? latencies[latencies.Count / 2]
            : (latencies[latencies.Count / 2 - 1] + latencies[latencies.Count / 2]) / 2.0;

        var total = relevant.Count;
        var failed = relevant.Count(e => !string.Equals(e.Status, "success", StringComparison.OrdinalIgnoreCase));
        var successRate = total > 0 ? (total - failed) / (double)total : 0;

        var avgCost = relevant
            .Where(e => e.EstimatedCostUsd.HasValue && e.TotalTokens > 0)
            .Select(e => e.EstimatedCostUsd!.Value / e.TotalTokens * 1000)
            .DefaultIfEmpty(0m)
            .Average();

        return new RoutingMetricsSnapshot(
            providerId,
            modelId,
            avgLatency,
            medianLatency,
            total,
            failed,
            successRate,
            avgCost > 0 ? avgCost : null);
    }

    public async Task<IReadOnlyList<ProviderLatencyRanking>> RankProvidersByLatencyAsync(
        IEnumerable<string> providerIds,
        CancellationToken ct = default)
    {
        var targetIds = providerIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = await GetRecentEntriesAsync(ct);

        var rankings = entries
            .Where(e => targetIds.Contains(e.ProviderId))
            .GroupBy(e => e.ProviderId, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var latencies = g.Select(e => e.DurationMs).ToList();
                var avgLatency = latencies.Average();
                var total = latencies.Count;
                var failed = g.Count(e => !string.Equals(e.Status, "success", StringComparison.OrdinalIgnoreCase));
                var successRate = total > 0 ? (total - failed) / (double)total : 0;

                return new ProviderLatencyRanking(
                    g.Key,
                    avgLatency,
                    total,
                    successRate);
            })
            .Where(r => r.ExecutionCount >= MinExecutionThreshold)
            .OrderBy(r => r.AverageLatencyMs)
            .ToList();

        return rankings;
    }

    public async Task<IReadOnlyList<ProviderCostRanking>> RankProvidersByCostAsync(
        IEnumerable<string> providerIds,
        CancellationToken ct = default)
    {
        var targetIds = providerIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = await GetRecentEntriesAsync(ct);

        var rankings = entries
            .Where(e => targetIds.Contains(e.ProviderId) && e.EstimatedCostUsd.HasValue && e.TotalTokens > 0)
            .GroupBy(e => e.ProviderId, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var costs = g.Select(e => e.EstimatedCostUsd!.Value / e.TotalTokens * 1000m).ToList();
                var avgCost = costs.Average();

                return new ProviderCostRanking(g.Key, avgCost, costs.Count);
            })
            .Where(r => r.ExecutionCount >= MinExecutionThreshold)
            .OrderBy(r => r.AverageCostPer1KTokens)
            .ToList();

        return rankings;
    }

    private async Task<IReadOnlyList<Domain.Governance.Entities.AiTokenUsageLedger>> GetRecentEntriesAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(MetricsLookbackWindow);
        try
        {
            return await _ledgerRepository.ListByPeriodAsync(cutoff, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load recent ledger entries for routing metrics.");
            return Array.Empty<Domain.Governance.Entities.AiTokenUsageLedger>();
        }
    }
}
