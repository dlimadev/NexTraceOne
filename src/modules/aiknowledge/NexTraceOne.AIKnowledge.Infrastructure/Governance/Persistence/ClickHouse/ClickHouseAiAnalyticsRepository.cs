using System.Text;
using System.Text.Json;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.ClickHouse;

/// <summary>
/// Implementação do repositório de analytics usando ClickHouse via HTTP API.
/// Fornece consultas analíticas de alta performance para métricas de IA.
///
/// Esta implementação usa o protocolo HTTP nativo do ClickHouse (porta 8123)
/// com formato JSONEachRow, evitando dependência do pacote ClickHouse.Client.
///
/// É ativada quando a connection string "AiAnalytics" está configurada
/// durante a instalação.
/// </summary>
internal sealed class ClickHouseAiAnalyticsRepository : IAiAnalyticsRepository
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ClickHouseConnectionOptions _options;
    private readonly ICurrentTenant _currentTenant;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClickHouseAiAnalyticsRepository(
        IHttpClientFactory httpClientFactory,
        ClickHouseConnectionOptions options,
        ICurrentTenant currentTenant)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _currentTenant = currentTenant;
        _baseUrl = $"http://{options.Host}:{options.Port}/?database={options.Database}";
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("ClickHouseAiAnalytics");
        return client;
    }

    private async Task ExecuteQueryAsync(string query, Dictionary<string, string>? parameters = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var url = BuildUrl(parameters);
        var content = new StringContent(query, Encoding.UTF8, "text/plain");
        var response = await client.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> QueryAsync(string query, Dictionary<string, string>? parameters = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var url = BuildUrl(parameters);
        var content = new StringContent(query, Encoding.UTF8, "text/plain");
        var response = await client.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private string BuildUrl(Dictionary<string, string>? parameters)
    {
        var builder = new StringBuilder(_baseUrl);
        if (parameters is not null && parameters.Count > 0)
        {
            // ClickHouse HTTP API: query parameters are passed as query string with param_ prefix
            foreach (var param in parameters)
            {
                builder.Append($"&param_{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}");
            }
        }
        return builder.ToString();
    }

    public async Task InsertTokenUsageAsync(TokenUsageRecord record, CancellationToken cancellationToken = default)
    {
        var jsonLine = JsonSerializer.Serialize(record, JsonOptions);
        // ClickHouse INSERT via JSONEachRow does not use user-supplied values in the query text
        // The JSON payload is data, not SQL syntax. No interpolation risk here.
        var query = "INSERT INTO ai_token_usage FORMAT JSONEachRow\n" + jsonLine;
        await ExecuteQueryAsync(query, cancellationToken: cancellationToken);
    }

    public async Task InsertTokenUsageBatchAsync(IEnumerable<TokenUsageRecord> records, CancellationToken cancellationToken = default)
    {
        var recordsList = records.ToList();
        if (recordsList.Count == 0) return;

        var jsonLines = recordsList.Select(r => JsonSerializer.Serialize(r, JsonOptions));
        var query = "INSERT INTO ai_token_usage FORMAT JSONEachRow\n" + string.Join("\n", jsonLines);
        await ExecuteQueryAsync(query, cancellationToken: cancellationToken);
    }

    public async Task<List<TokenUsageMetrics>> GetTokenUsageMetricsAsync(DateTime from, DateTime to, Guid? modelId = null)
    {
        var tenantId = _currentTenant.Id;
        var parameters = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["fromDate"] = from.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["toDate"] = to.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
        };

        var whereClause = "WHERE TenantId = {tenantId:String} AND Timestamp >= {fromDate:DateTime} AND Timestamp <= {toDate:DateTime}";
        if (modelId.HasValue)
        {
            parameters["modelId"] = modelId.Value.ToString();
            whereClause += " AND ModelId = {modelId:String}";
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

        var result = await QueryAsync(query, parameters);
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
        var tenantId = _currentTenant.Id;
        var parameters = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["fromDate"] = from.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["toDate"] = to.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
        };

        var whereClause = "WHERE TenantId = {tenantId:String} AND Timestamp >= {fromDate:DateTime} AND Timestamp <= {toDate:DateTime}";
        if (agentId.HasValue)
        {
            parameters["agentId"] = agentId.Value.ToString();
            whereClause += " AND AgentId = {agentId:String}";
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

        var result = await QueryAsync(query, parameters);
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
        var tenantId = _currentTenant.Id;
        var parameters = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["fromDate"] = from.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["toDate"] = to.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
        };

        var whereClause = "WHERE TenantId = {tenantId:String} AND Timestamp >= {fromDate:DateTime} AND Timestamp <= {toDate:DateTime}";
        if (modelId.HasValue)
        {
            parameters["modelId"] = modelId.Value.ToString();
            whereClause += " AND ModelId = {modelId:String}";
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

        var result = await QueryAsync(query, parameters);
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
        var tenantId = _currentTenant.Id;
        var parameters = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["fromDate"] = from.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["toDate"] = to.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
        };

        var query = @"
            SELECT sum(CostUSD)
            FROM ai_token_usage
            WHERE TenantId = {tenantId:String} AND Timestamp >= {fromDate:DateTime} AND Timestamp <= {toDate:DateTime}
            FORMAT JSON";

        var result = await QueryAsync(query, parameters);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");

        if (rows.GetArrayLength() == 0) return 0m;

        var value = rows[0].GetProperty("sum(CostUSD)");
        return value.ValueKind == JsonValueKind.Null ? 0m : value.GetDecimal();
    }

    public async Task<long> GetTotalAgentExecutionsAsync(DateTime from, DateTime to)
    {
        var tenantId = _currentTenant.Id;
        var parameters = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["fromDate"] = from.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["toDate"] = to.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
        };

        var query = @"
            SELECT count()
            FROM ai_agent_executions
            WHERE TenantId = {tenantId:String} AND Timestamp >= {fromDate:DateTime} AND Timestamp <= {toDate:DateTime}
            FORMAT JSON";

        var result = await QueryAsync(query, parameters);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");

        if (rows.GetArrayLength() == 0) return 0L;

        return rows[0].GetProperty("count()").GetInt64();
    }

    public async Task<double> GetAgentSuccessRateAsync(DateTime from, DateTime to)
    {
        var tenantId = _currentTenant.Id;
        var parameters = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["fromDate"] = from.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["toDate"] = to.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
        };

        var query = @"
            SELECT
                (sumIf(1, Status = 'Success') / count()) * 100 as SuccessRate
            FROM ai_agent_executions
            WHERE TenantId = {tenantId:String} AND Timestamp >= {fromDate:DateTime} AND Timestamp <= {toDate:DateTime}
            FORMAT JSON";

        var result = await QueryAsync(query, parameters);
        var jsonDoc = JsonDocument.Parse(result);
        var rows = jsonDoc.RootElement.GetProperty("data");

        if (rows.GetArrayLength() == 0) return 0.0;

        var value = rows[0].GetProperty("SuccessRate");
        return value.ValueKind == JsonValueKind.Null ? 0.0 : value.GetDouble();
    }
}
