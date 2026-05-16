using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

/// <summary>
/// Implementação nula do repositório de analytics.
/// Usada quando o usuário NÃO escolheu ClickHouse durante a instalação.
/// Retorna coleções vazias e valores padrão para evitar NullReferenceException.
/// </summary>
internal sealed class NullAiAnalyticsRepository : IAiAnalyticsRepository
{
    private static readonly Task<List<TokenUsageMetrics>> EmptyTokenMetrics = Task.FromResult(new List<TokenUsageMetrics>());
    private static readonly Task<List<AgentExecutionMetrics>> EmptyAgentMetrics = Task.FromResult(new List<AgentExecutionMetrics>());
    private static readonly Task<List<ModelPerformanceMetrics>> EmptyModelMetrics = Task.FromResult(new List<ModelPerformanceMetrics>());

    public Task InsertTokenUsageAsync(TokenUsageRecord record, CancellationToken cancellationToken = default)
    {
        // No-op: ClickHouse não está configurado
        return Task.CompletedTask;
    }

    public Task InsertTokenUsageBatchAsync(IEnumerable<TokenUsageRecord> records, CancellationToken cancellationToken = default)
    {
        // No-op: ClickHouse não está configurado
        return Task.CompletedTask;
    }

    public Task<List<TokenUsageMetrics>> GetTokenUsageMetricsAsync(DateTime from, DateTime to, Guid? modelId = null)
    {
        return EmptyTokenMetrics;
    }

    public Task<List<AgentExecutionMetrics>> GetAgentExecutionMetricsAsync(DateTime from, DateTime to, Guid? agentId = null)
    {
        return EmptyAgentMetrics;
    }

    public Task<List<ModelPerformanceMetrics>> GetModelPerformanceMetricsAsync(DateTime from, DateTime to, Guid? modelId = null)
    {
        return EmptyModelMetrics;
    }

    public Task<decimal> GetTotalTokenCostAsync(DateTime from, DateTime to)
    {
        return Task.FromResult(0m);
    }

    public Task<long> GetTotalAgentExecutionsAsync(DateTime from, DateTime to)
    {
        return Task.FromResult(0L);
    }

    public Task<double> GetAgentSuccessRateAsync(DateTime from, DateTime to)
    {
        return Task.FromResult(0.0);
    }
}
