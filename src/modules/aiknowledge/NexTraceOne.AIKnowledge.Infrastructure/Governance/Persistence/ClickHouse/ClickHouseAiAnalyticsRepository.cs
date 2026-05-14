using System.Text;
using System.Text.Json;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.ClickHouse;

/// <summary>
/// Implementação do repositório de analytics usando ClickHouse via HTTP API.
/// Fornece consultas analíticas de alta performance para métricas de IA.
/// 
/// Esta implementação usa o protocolo HTTP nativo do ClickHouse (porta 8123)
/// com formato JSONEachRow, evitando dependência do pacote ClickHouse.Client.
/// 
/// É ativada quando a connection string "AiAnalytics" está configurada
/// no appsettings.json durante a instalação.
/// </summary>
internal sealed class ClickHouseAiAnalyticsRepository : IAiAnalyticsRepository, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private bool _disposed = false;

    public ClickHouseAiAnalyticsRepository(string connectionString)
    {
        // Connection string format: "Host=localhost;Port=8123;Database=ai_analytics;Username=default;Password="
        var parts = connectionString.Split(';');
        var host = ExtractValue(parts, "Host") ?? "localhost";
        var port = ExtractValue(parts, "Port") ?? "8123";
        var database = ExtractValue(parts, "Database") ?? "default";
        var username = ExtractValue(parts, "Username");
        var password = ExtractValue(parts, "Password");

        _baseUrl = $"http://{host}:{port}/?database={database}";
        
        _httpClient = new HttpClient();
        if (!string.IsNullOrEmpty(username))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password ?? ""}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        }
    }

    private static string? ExtractValue(string[] parts, string key)
    {
        var part = parts.FirstOrDefault(p => p.Trim().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
        return part?.Substring(part.IndexOf('=') + 1).Trim();
    }

    private async Task ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(query, Encoding.UTF8, "text/plain");
        var response = await _httpClient.PostAsync(_baseUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(query, Encoding.UTF8, "text/plain");
        var response = await _httpClient.PostAsync(_baseUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task InsertTokenUsageAsync(TokenUsageRecord record)
    {
        var query = $@"
            INSERT INTO ai_token_usage FORMAT JSONEachRow
            {{""Id"":""{record.Id}"",""TenantId"":""{record.TenantId}"",""ModelId"":""{record.ModelId}"",""AgentId"":""{record.AgentId ?? Guid.Empty}"",""PromptTokens"":{record.PromptTokens},""CompletionTokens"":{record.CompletionTokens},""TotalTokens"":{record.TotalTokens},""CostUSD"":{record.CostUSD.ToString(System.Globalization.CultureInfo.InvariantCulture)},""Timestamp"":""{record.Timestamp:yyyy-MM-dd HH:mm:ss}"",""OperationType"":""{record.OperationType}"",""UserId"":""{record.UserId}""}}";

        await ExecuteQueryAsync(query);
    }

    public async Task InsertTokenUsageBatchAsync(IEnumerable<TokenUsageRecord> records)
    {
        var recordsList = records.ToList();
        if (recordsList.Count == 0) return;

        var jsonLines = recordsList.Select(r => 
            $@"{{""Id"":""{r.Id}"",""TenantId"":""{r.TenantId}"",""ModelId"":""{r.ModelId}"",""AgentId"":""{r.AgentId ?? Guid.Empty}"",""PromptTokens"":{r.PromptTokens},""CompletionTokens"":{r.CompletionTokens},""TotalTokens"":{r.TotalTokens},""CostUSD"":{r.CostUSD.ToString(System.Globalization.CultureInfo.InvariantCulture)},""Timestamp"":""{r.Timestamp:yyyy-MM-dd HH:mm:ss}"",""OperationType"":""{r.OperationType}"",""UserId"":""{r.UserId}""}}");

        var query = $"INSERT INTO ai_token_usage FORMAT JSONEachRow\n{string.Join("\n", jsonLines)}";
        await ExecuteQueryAsync(query);
    }

    public async Task<List<TokenUsageMetrics>> GetTokenUsageMetricsAsync(DateTime from, DateTime to, Guid? modelId = null)
    {
        var whereClause = $"WHERE Timestamp >= '{from:yyyy-MM-dd HH:mm:ss}' AND Timestamp <= '{to:yyyy-MM-dd HH:mm:ss}'";
        if (modelId.HasValue)
        {
            whereClause += $" AND ModelId = '{modelId.Value}'";
        }

        var query = $@"
            SELECT 
                ModelId,
                any(ModelName) as ModelName,
                count() as TotalRequests,
                sum(PromptTokens) as TotalPromptTokens,
                sum(CompletionTokens) as TotalCompletionTokens,
                sum(TotalTokens) as TotalTokens,
                sum(CostUSD) as TotalCostUSD,
                avg(TotalTokens) as AvgTokensPerRequest,
                min(Timestamp) as PeriodStart,
                max(Timestamp) as PeriodEnd
            FROM ai_token_usage
            {whereClause}
            GROUP BY ModelId
            ORDER BY TotalTokens DESC
            FORMAT JSON";

        var result = await QueryAsync(query);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");

        var metrics = new List<TokenUsageMetrics>();
        foreach (var row in rows.EnumerateArray())
        {
            metrics.Add(new TokenUsageMetrics(
                ModelId: Guid.Parse(row.GetProperty("ModelId").GetString()!),
                ModelName: row.GetProperty("ModelName").GetString()!,
                TotalRequests: row.GetProperty("TotalRequests").GetInt64(),
                TotalPromptTokens: row.GetProperty("TotalPromptTokens").GetInt64(),
                TotalCompletionTokens: row.GetProperty("TotalCompletionTokens").GetInt64(),
                TotalTokens: row.GetProperty("TotalTokens").GetInt64(),
                TotalCostUSD: row.GetProperty("TotalCostUSD").GetDecimal(),
                AvgTokensPerRequest: row.GetProperty("AvgTokensPerRequest").GetDouble(),
                PeriodStart: DateTime.Parse(row.GetProperty("PeriodStart").GetString()!),
                PeriodEnd: DateTime.Parse(row.GetProperty("PeriodEnd").GetString()!)));
        }

        return metrics;
    }

    public async Task<List<AgentExecutionMetrics>> GetAgentExecutionMetricsAsync(DateTime from, DateTime to, Guid? agentId = null)
    {
        var whereClause = $"WHERE Timestamp >= '{from:yyyy-MM-dd HH:mm:ss}' AND Timestamp <= '{to:yyyy-MM-dd HH:mm:ss}'";
        if (agentId.HasValue)
        {
            whereClause += $" AND AgentId = '{agentId.Value}'";
        }

        var query = $@"
            SELECT 
                AgentId,
                any(AgentName) as AgentName,
                count() as TotalExecutions,
                sumIf(1, Status = 'Success') as SuccessfulExecutions,
                sumIf(1, Status = 'Failed') as FailedExecutions,
                (SuccessfulExecutions / TotalExecutions) * 100 as SuccessRate,
                avg(DurationMs) as AvgDuration,
                quantile(0.95)(DurationMs) as P95Duration,
                sum(CostUSD) as TotalCostUSD,
                min(Timestamp) as PeriodStart,
                max(Timestamp) as PeriodEnd
            FROM ai_agent_executions
            {whereClause}
            GROUP BY AgentId
            ORDER BY TotalExecutions DESC
            FORMAT JSON";

        var result = await QueryAsync(query);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");

        var metrics = new List<AgentExecutionMetrics>();
        foreach (var row in rows.EnumerateArray())
        {
            metrics.Add(new AgentExecutionMetrics(
                AgentId: Guid.Parse(row.GetProperty("AgentId").GetString()!),
                AgentName: row.GetProperty("AgentName").GetString()!,
                TotalExecutions: row.GetProperty("TotalExecutions").GetInt64(),
                SuccessfulExecutions: row.GetProperty("SuccessfulExecutions").GetInt64(),
                FailedExecutions: row.GetProperty("FailedExecutions").GetInt64(),
                SuccessRate: row.GetProperty("SuccessRate").GetDouble(),
                AvgDuration: TimeSpan.FromMilliseconds(row.GetProperty("AvgDuration").GetDouble()),
                P95Duration: TimeSpan.FromMilliseconds(row.GetProperty("P95Duration").GetDouble()),
                TotalCostUSD: row.GetProperty("TotalCostUSD").GetDecimal(),
                PeriodStart: DateTime.Parse(row.GetProperty("PeriodStart").GetString()!),
                PeriodEnd: DateTime.Parse(row.GetProperty("PeriodEnd").GetString()!)));
        }

        return metrics;
    }

    public async Task<List<ModelPerformanceMetrics>> GetModelPerformanceMetricsAsync(DateTime from, DateTime to, Guid? modelId = null)
    {
        var whereClause = $"WHERE Timestamp >= '{from:yyyy-MM-dd HH:mm:ss}' AND Timestamp <= '{to:yyyy-MM-dd HH:mm:ss}'";
        if (modelId.HasValue)
        {
            whereClause += $" AND ModelId = '{modelId.Value}'";
        }

        var query = $@"
            SELECT 
                ModelId,
                any(ModelName) as ModelName,
                count() as TotalRequests,
                avg(LatencyMs) as AvgLatencyMs,
                quantile(0.95)(LatencyMs) as P95LatencyMs,
                quantile(0.99)(LatencyMs) as P99LatencyMs,
                (sumIf(1, IsError = 1) / count()) * 100 as ErrorRate,
                count() / greatest(date_diff('minute', min(Timestamp), max(Timestamp)), 1) as RequestsPerMinute,
                min(Timestamp) as PeriodStart,
                max(Timestamp) as PeriodEnd
            FROM ai_model_performance
            {whereClause}
            GROUP BY ModelId
            ORDER BY TotalRequests DESC
            FORMAT JSON";

        var result = await QueryAsync(query);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");

        var metrics = new List<ModelPerformanceMetrics>();
        foreach (var row in rows.EnumerateArray())
        {
            metrics.Add(new ModelPerformanceMetrics(
                ModelId: Guid.Parse(row.GetProperty("ModelId").GetString()!),
                ModelName: row.GetProperty("ModelName").GetString()!,
                TotalRequests: row.GetProperty("TotalRequests").GetInt64(),
                AvgLatencyMs: row.GetProperty("AvgLatencyMs").GetDouble(),
                P95LatencyMs: row.GetProperty("P95LatencyMs").GetDouble(),
                P99LatencyMs: row.GetProperty("P99LatencyMs").GetDouble(),
                ErrorRate: row.GetProperty("ErrorRate").GetDouble(),
                RequestsPerMinute: row.GetProperty("RequestsPerMinute").GetInt64(),
                PeriodStart: DateTime.Parse(row.GetProperty("PeriodStart").GetString()!),
                PeriodEnd: DateTime.Parse(row.GetProperty("PeriodEnd").GetString()!)));
        }

        return metrics;
    }

    public async Task<decimal> GetTotalTokenCostAsync(DateTime from, DateTime to)
    {
        var query = $@"
            SELECT sum(CostUSD) 
            FROM ai_token_usage 
            WHERE Timestamp >= '{from:yyyy-MM-dd HH:mm:ss}' AND Timestamp <= '{to:yyyy-MM-dd HH:mm:ss}'
            FORMAT JSON";

        var result = await QueryAsync(query);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");
        
        if (rows.GetArrayLength() == 0) return 0m;
        
        var value = rows[0].GetProperty("sum(CostUSD)");
        return value.ValueKind == JsonValueKind.Null ? 0m : value.GetDecimal();
    }

    public async Task<long> GetTotalAgentExecutionsAsync(DateTime from, DateTime to)
    {
        var query = $@"
            SELECT count() 
            FROM ai_agent_executions 
            WHERE Timestamp >= '{from:yyyy-MM-dd HH:mm:ss}' AND Timestamp <= '{to:yyyy-MM-dd HH:mm:ss}'
            FORMAT JSON";

        var result = await QueryAsync(query);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");
        
        if (rows.GetArrayLength() == 0) return 0L;
        
        return rows[0].GetProperty("count()").GetInt64();
    }

    public async Task<double> GetAgentSuccessRateAsync(DateTime from, DateTime to)
    {
        var query = $@"
            SELECT 
                (sumIf(1, Status = 'Success') / count()) * 100 as SuccessRate
            FROM ai_agent_executions
            WHERE Timestamp >= '{from:yyyy-MM-dd HH:mm:ss}' AND Timestamp <= '{to:yyyy-MM-dd HH:mm:ss}'
            FORMAT JSON";

        var result = await QueryAsync(query);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");
        
        if (rows.GetArrayLength() == 0) return 0.0;
        
        var value = rows[0].GetProperty("SuccessRate");
        return value.ValueKind == JsonValueKind.Null ? 0.0 : value.GetDouble();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
