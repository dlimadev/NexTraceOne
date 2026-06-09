using System.Text;
using System.Text.Json;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiUsageDashboard;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.ClickHouse;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

/// <summary>
/// Repositório de entradas de auditoria de uso de IA via ClickHouse.
/// Usa o protocolo HTTP nativo do ClickHouse (porta 8123) com formato JSONEachRow.
/// Tabela: ai_usage_entries na database configurada via connection string "AiAnalytics".
/// Fase 2.5: Substitui AiUsageEntryRepository (EF Core / PostgreSQL).
/// </summary>
internal sealed class ClickHouseAiUsageEntryRepository : IAiUsageEntryRepository
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ClickHouseConnectionOptions _options;
    private readonly ICurrentTenant _currentTenant;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClickHouseAiUsageEntryRepository(
        IHttpClientFactory httpClientFactory,
        ClickHouseConnectionOptions options,
        ICurrentTenant currentTenant)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _currentTenant = currentTenant;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("ClickHouseAiAnalytics");

    private string BuildUrl(Dictionary<string, string>? parameters = null)
    {
        var sb = new StringBuilder($"http://{_options.Host}:{_options.Port}/?database={_options.Database}");
        if (parameters is not null)
            foreach (var (key, value) in parameters)
                sb.Append($"&param_{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        return sb.ToString();
    }

    private async Task ExecuteAsync(string sql, Dictionary<string, string>? parameters = null, CancellationToken ct = default)
    {
        var client = CreateClient();
        using var content = new StringContent(sql, Encoding.UTF8, "text/plain");
        var response = await client.PostAsync(BuildUrl(parameters), content, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> QueryAsync(string sql, Dictionary<string, string> parameters, CancellationToken ct)
    {
        var client = CreateClient();
        using var content = new StringContent(sql, Encoding.UTF8, "text/plain");
        var response = await client.PostAsync(BuildUrl(parameters), content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task AddAsync(AIUsageEntry entry, CancellationToken ct)
    {
        var row = new
        {
            Id = entry.Id.Value.ToString(),
            TenantId = _currentTenant.Id.ToString(),
            entry.UserId,
            entry.UserDisplayName,
            ModelId = entry.ModelId.ToString(),
            entry.ModelName,
            entry.Provider,
            IsInternal = entry.IsInternal ? 1 : 0,
            IsExternal = entry.IsExternal ? 1 : 0,
            Timestamp = entry.Timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture),
            entry.PromptTokens,
            entry.CompletionTokens,
            entry.TotalTokens,
            PolicyId = entry.PolicyId?.ToString(),
            entry.PolicyName,
            Result = (int)entry.Result,
            ConversationId = entry.ConversationId?.ToString(),
            entry.ContextScope,
            ClientType = (int)entry.ClientType,
            entry.CorrelationId,
            entry.CostUsd,
            entry.DurationMs,
            SafetyFilterTriggered = entry.SafetyFilterTriggered ? 1 : 0,
            entry.ErrorCode,
            IsStreaming = entry.IsStreaming ? 1 : 0
        };

        var json = JsonSerializer.Serialize(row, JsonOptions);
        await ExecuteAsync("INSERT INTO ai_usage_entries FORMAT JSONEachRow\n" + json, ct: ct);
    }

    public async Task<IReadOnlyList<AIUsageEntry>> ListAsync(
        string? userId, Guid? modelId, DateTimeOffset? startDate, DateTimeOffset? endDate,
        UsageResult? result, AIClientType? clientType, int pageSize, CancellationToken ct)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tenantId"] = _currentTenant.Id.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        var where = new StringBuilder("WHERE TenantId = {tenantId:String}");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            parameters["userId"] = userId;
            where.Append(" AND UserId = {userId:String}");
        }
        if (modelId.HasValue)
        {
            parameters["modelId"] = modelId.Value.ToString();
            where.Append(" AND ModelId = {modelId:String}");
        }
        if (startDate.HasValue)
        {
            parameters["startDate"] = startDate.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            where.Append(" AND Timestamp >= {startDate:DateTime64}");
        }
        if (endDate.HasValue)
        {
            parameters["endDate"] = endDate.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            where.Append(" AND Timestamp <= {endDate:DateTime64}");
        }
        if (result.HasValue)
        {
            parameters["result"] = ((int)result.Value).ToString();
            where.Append(" AND Result = {result:Int32}");
        }
        if (clientType.HasValue)
        {
            parameters["clientType"] = ((int)clientType.Value).ToString();
            where.Append(" AND ClientType = {clientType:Int32}");
        }

        var sql = $@"
            SELECT Id, UserId, UserDisplayName, ModelId, ModelName, Provider,
                   IsInternal, Timestamp, PromptTokens, CompletionTokens, TotalTokens,
                   PolicyId, PolicyName, Result, ConversationId, ContextScope, ClientType, CorrelationId
            FROM ai_usage_entries
            {where}
            ORDER BY Timestamp DESC
            LIMIT {{pageSize:UInt32}}
            FORMAT JSON";

        var raw = await QueryAsync(sql, parameters, ct);
        var doc = JsonDocument.Parse(raw);
        var rows = doc.RootElement.GetProperty("data");

        var entries = new List<AIUsageEntry>();
        foreach (var row in rows.EnumerateArray())
        {
            entries.Add(AIUsageEntry.Reconstitute(
                AIUsageEntryId.From(Guid.Parse(row.GetProperty("Id").GetString()!)),
                row.GetProperty("UserId").GetString()!,
                row.GetProperty("UserDisplayName").GetString()!,
                Guid.Parse(row.GetProperty("ModelId").GetString()!),
                row.GetProperty("ModelName").GetString()!,
                row.GetProperty("Provider").GetString()!,
                row.GetProperty("IsInternal").GetInt32() == 1,
                DateTimeOffset.Parse(row.GetProperty("Timestamp").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                row.GetProperty("PromptTokens").GetInt32(),
                row.GetProperty("CompletionTokens").GetInt32(),
                row.GetProperty("TotalTokens").GetInt32(),
                ParseNullableGuid(row.GetProperty("PolicyId")),
                ParseNullableString(row.GetProperty("PolicyName")),
                (UsageResult)row.GetProperty("Result").GetInt32(),
                ParseNullableGuid(row.GetProperty("ConversationId")),
                row.GetProperty("ContextScope").GetString()!,
                (AIClientType)row.GetProperty("ClientType").GetInt32(),
                row.GetProperty("CorrelationId").GetString()!));
        }

        return entries;
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        var cutoffStr = cutoff.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        var parameters = new Dictionary<string, string> { ["cutoff"] = cutoffStr };

        var countSql = "SELECT count() AS cnt FROM ai_usage_entries WHERE Timestamp < {cutoff:DateTime64} FORMAT JSON";
        var raw = await QueryAsync(countSql, parameters, ct);
        var doc = JsonDocument.Parse(raw);
        var count = (int)doc.RootElement.GetProperty("data")[0].GetProperty("cnt").GetInt64();

        await ExecuteAsync("ALTER TABLE ai_usage_entries DELETE WHERE Timestamp < {cutoff:DateTime64}", parameters, ct);

        return count;
    }

    public async Task<IReadOnlyList<AiUsageAggregate>> GetAggregatedUsageAsync(
        Guid tenantId, DateTimeOffset start, DateTimeOffset end, string groupBy, int top, CancellationToken ct)
    {
        var groupColumn = groupBy.ToLowerInvariant() switch
        {
            "user" => "UserId",
            "provider" => "Provider",
            _ => "ModelId"
        };

        var parameters = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["start"] = start.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["end"] = end.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["top"] = top.ToString()
        };

        var sql = $@"
            SELECT {groupColumn} AS DimensionKey, {groupColumn} AS DimensionLabel,
                   sum(TotalTokens) AS TotalTokens, toInt32(count()) AS TotalRequests
            FROM ai_usage_entries
            WHERE TenantId = {{tenantId:String}}
              AND Timestamp >= {{start:DateTime64}}
              AND Timestamp <= {{end:DateTime64}}
            GROUP BY {groupColumn}
            ORDER BY TotalTokens DESC
            LIMIT {{top:UInt32}}
            FORMAT JSON";

        var raw = await QueryAsync(sql, parameters, ct);
        var doc = JsonDocument.Parse(raw);
        var rows = doc.RootElement.GetProperty("data");

        var result = new List<AiUsageAggregate>();
        foreach (var row in rows.EnumerateArray())
        {
            result.Add(new AiUsageAggregate(
                row.GetProperty("DimensionKey").GetString()!,
                row.GetProperty("DimensionLabel").GetString()!,
                row.GetProperty("TotalTokens").GetInt64(),
                row.GetProperty("TotalRequests").GetInt32(),
                null));
        }

        return result;
    }

    private static Guid? ParseNullableGuid(JsonElement element)
        => element.ValueKind == JsonValueKind.Null ? null
            : Guid.TryParse(element.GetString(), out var g) ? g : null;

    private static string? ParseNullableString(JsonElement element)
        => element.ValueKind == JsonValueKind.Null ? null : element.GetString();
}
