namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Interface para repositório de analytics de IA usando banco de dados columnar (ClickHouse).
/// Fornece consultas analíticas de alta performance para métricas de uso de agentes, modelos e tokens.
/// 
/// Esta interface é implementada apenas quando o usuário escolhe ClickHouse durante a instalação.
/// Se não configurado, usa-se NullAiAnalyticsRepository (retorna coleções vazias).
/// </summary>
public interface IAiAnalyticsRepository
{
    /// <summary>Insere registro de uso de token no ClickHouse.</summary>
    Task InsertTokenUsageAsync(TokenUsageRecord record, CancellationToken cancellationToken = default);

    /// <summary>Insere batch de registros de uso para melhor performance.</summary>
    Task InsertTokenUsageBatchAsync(IEnumerable<TokenUsageRecord> records, CancellationToken cancellationToken = default);

    /// <summary>Obtém métricas de uso de tokens agrupadas por modelo e período.</summary>
    Task<List<TokenUsageMetrics>> GetTokenUsageMetricsAsync(DateTime from, DateTime to, Guid? modelId = null);

    /// <summary>Obtém métricas de execução de agentes (duração, sucesso/falha, custo).</summary>
    Task<List<AgentExecutionMetrics>> GetAgentExecutionMetricsAsync(DateTime from, DateTime to, Guid? agentId = null);

    /// <summary>Obtém métricas de performance de modelos (latência, throughput, error rate).</summary>
    Task<List<ModelPerformanceMetrics>> GetModelPerformanceMetricsAsync(DateTime from, DateTime to, Guid? modelId = null);

    /// <summary>Calcula custo total de tokens em um período.</summary>
    Task<decimal> GetTotalTokenCostAsync(DateTime from, DateTime to);

    /// <summary>Conta total de execuções de agentes em um período.</summary>
    Task<long> GetTotalAgentExecutionsAsync(DateTime from, DateTime to);

    /// <summary>Calcula taxa de sucesso de agentes percentual em um período.</summary>
    Task<double> GetAgentSuccessRateAsync(DateTime from, DateTime to);
}

/// <summary>
/// Registro de uso de tokens (estrutura para ClickHouse).
/// </summary>
public sealed record TokenUsageRecord(
    Guid Id,
    Guid TenantId,
    Guid ModelId,
    Guid? AgentId,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal CostUSD,
    DateTime Timestamp,
    string OperationType,
    string UserId);

/// <summary>
/// Métricas de uso de tokens agregadas.
/// </summary>
public sealed record TokenUsageMetrics(
    Guid ModelId,
    string ModelName,
    long TotalRequests,
    long TotalPromptTokens,
    long TotalCompletionTokens,
    long TotalTokens,
    decimal TotalCostUSD,
    double AvgTokensPerRequest,
    DateTime PeriodStart,
    DateTime PeriodEnd);

/// <summary>
/// Métricas de execução de agentes.
/// </summary>
public sealed record AgentExecutionMetrics(
    Guid AgentId,
    string AgentName,
    long TotalExecutions,
    long SuccessfulExecutions,
    long FailedExecutions,
    double SuccessRate,
    TimeSpan AvgDuration,
    TimeSpan P95Duration,
    decimal TotalCostUSD,
    DateTime PeriodStart,
    DateTime PeriodEnd);

/// <summary>
/// Métricas de performance de modelos.
/// </summary>
public sealed record ModelPerformanceMetrics(
    Guid ModelId,
    string ModelName,
    long TotalRequests,
    double AvgLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    double ErrorRate,
    long RequestsPerMinute,
    DateTime PeriodStart,
    DateTime PeriodEnd);
