using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Repositories;

/// <summary>
/// ClickHouse-backed implementation of IAnalyticsEventRepository.
/// Reads from nextraceone_analytics.pan_events (written by IAnalyticsWriter.WriteProductEventAsync).
/// Writes (AddAsync) still go to PostgreSQL via IAnalyticsEventRepository delegation.
/// Activated when Telemetry:ObservabilityProvider:Provider = "ClickHouse".
/// </summary>
internal sealed class ClickHouseAnalyticsEventRepository(
    HttpClient httpClient,
    IOptions<ClickHouseAnalyticsOptions> options,
    IAnalyticsEventRepository fallbackRepository,
    ICurrentTenant currentTenant,
    ILogger<ClickHouseAnalyticsEventRepository> logger) : IAnalyticsEventRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private string Endpoint => options.Value.Endpoint;

    // AddAsync delegates to the PostgreSQL fallback repository.
    public async Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken ct)
        => await fallbackRepository.AddAsync(analyticsEvent, ct);

    public async Task<long> CountAsync(
        string? persona, ProductModule? module, string? teamId, string? domainId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, module: module, teamId: teamId,
            domainId: domainId, from: from, to: to);
        return await ExecuteScalarAsync<long>($"SELECT count() FROM pan_events {where}", ct,
            persona: persona, teamId: teamId, domainId: domainId, from: from, to: to);
    }

    public async Task<long> CountByEventTypeAsync(
        AnalyticsEventType eventType, string? persona, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, eventType: (int)eventType, from: from, to: to);
        return await ExecuteScalarAsync<long>($"SELECT count() FROM pan_events {where}", ct,
            persona: persona, from: from, to: to);
    }

    public async Task<int> CountUniqueUsersAsync(
        string? persona, ProductModule? module, string? teamId, string? domainId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, module: module, teamId: teamId,
            domainId: domainId, from: from, to: to);
        return await ExecuteScalarAsync<int>($"SELECT count(DISTINCT user_id) FROM pan_events {where}", ct,
            persona: persona, teamId: teamId, domainId: domainId, from: from, to: to);
    }

    public async Task<int> CountActivePersonasAsync(
        string? module, string? teamId, string? domainId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(module: ParseModule(module), teamId: teamId, domainId: domainId, from: from, to: to);
        return await ExecuteScalarAsync<int>($"SELECT count(DISTINCT persona) FROM pan_events {where}", ct,
            teamId: teamId, domainId: domainId, from: from, to: to);
    }

    public async Task<IReadOnlyList<ModuleUsageRow>> GetTopModulesAsync(
        string? persona, string? teamId, string? domainId,
        DateTimeOffset from, DateTimeOffset to, int top, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, teamId: teamId, domainId: domainId, from: from, to: to);
        var sql = $"""
            SELECT module, count() AS event_count, count(DISTINCT user_id) AS unique_users
            FROM pan_events {where}
            GROUP BY module ORDER BY event_count DESC LIMIT {top}
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<ModuleRow>(sql, ct,
            persona: persona, teamId: teamId, domainId: domainId, from: from, to: to);
        return rows.Select(r => new ModuleUsageRow(
            Module: (ProductModule)r.Module,
            EventCount: r.EventCount,
            UniqueUsers: r.UniqueUsers)).ToList();
    }

    public async Task<IReadOnlyList<ModuleAdoptionRow>> GetModuleAdoptionAsync(
        string? persona, string? teamId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, teamId: teamId, from: from, to: to);
        var sql = $"""
            SELECT module, count() AS total_actions, count(DISTINCT user_id) AS unique_users
            FROM pan_events {where}
            GROUP BY module ORDER BY total_actions DESC
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<ModuleRow>(sql, ct,
            persona: persona, teamId: teamId, from: from, to: to);
        return rows.Select(r => new ModuleAdoptionRow(
            Module: (ProductModule)r.Module,
            TotalActions: r.EventCount,
            UniqueUsers: r.UniqueUsers)).ToList();
    }

    public async Task<IReadOnlyList<ModuleFeatureCountRow>> GetFeatureCountsAsync(
        string? persona, string? teamId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, teamId: teamId, from: from, to: to);
        var sql = $"""
            SELECT module, feature, count() AS cnt
            FROM pan_events {where}
            GROUP BY module, feature ORDER BY cnt DESC
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<FeatureCountRow>(sql, ct,
            persona: persona, teamId: teamId, from: from, to: to);
        return rows.Select(r => new ModuleFeatureCountRow(
            Module: (ProductModule)r.Module,
            Feature: r.Feature,
            Count: r.Cnt)).ToList();
    }

    public async Task<IReadOnlyList<SessionEventRow>> ListSessionEventsAsync(
        string? persona, string? teamId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, teamId: teamId, from: from, to: to);
        var sql = $"""
            SELECT session_id, event_type, occurred_at
            FROM pan_events {where}
            ORDER BY occurred_at
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<SessionEventRecord>(sql, ct,
            persona: persona, teamId: teamId, from: from, to: to);
        return rows.Select(r => new SessionEventRow(
            SessionId: r.SessionId,
            EventType: (AnalyticsEventType)r.EventType,
            OccurredAt: r.OccurredAt)).ToList();
    }

    public async Task<IReadOnlyList<PersonaBreakdownRow>> GetPersonaBreakdownAsync(
        string? teamId, string? domainId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(teamId: teamId, domainId: domainId, from: from, to: to);
        var sql = $"""
            SELECT persona, count() AS event_count, count(DISTINCT user_id) AS unique_users
            FROM pan_events {where}
            GROUP BY persona ORDER BY event_count DESC
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<PersonaRow>(sql, ct,
            teamId: teamId, domainId: domainId, from: from, to: to);
        return rows.Select(r => new PersonaBreakdownRow(
            Persona: r.Persona,
            EventCount: r.EventCount,
            UniqueUsers: r.UniqueUsers)).ToList();
    }

    public async Task<IReadOnlyList<EventTypeCountRow>> GetTopEventTypesAsync(
        string? persona, DateTimeOffset from, DateTimeOffset to, int top, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, from: from, to: to);
        var sql = $"""
            SELECT event_type, count() AS cnt
            FROM pan_events {where}
            GROUP BY event_type ORDER BY cnt DESC LIMIT {top}
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<EventTypeCountRecord>(sql, ct,
            persona: persona, from: from, to: to);
        return rows.Select(r => new EventTypeCountRow(
            EventType: (AnalyticsEventType)r.EventType,
            Count: r.Cnt)).ToList();
    }

    public async Task<IReadOnlyList<AnalyticsEventType>> GetDistinctEventTypesAsync(
        string? persona, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, from: from, to: to);
        var sql = $"""
            SELECT DISTINCT event_type FROM pan_events {where}
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<DistinctEventTypeRow>(sql, ct,
            persona: persona, from: from, to: to);
        return rows.Select(r => (AnalyticsEventType)r.EventType).ToList();
    }

    public async Task<IReadOnlyList<EventTypeUserCountRow>> CountUsersByEventTypeAsync(
        AnalyticsEventType[] eventTypes, string? persona, string? teamId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        if (eventTypes.Length == 0) return [];
        var inClause = string.Join(",", eventTypes.Select(e => (int)e));
        var where = BuildWhereClause(persona: persona, teamId: teamId, from: from, to: to,
            extra: $"AND event_type IN ({inClause})");
        var sql = $"""
            SELECT event_type, count(DISTINCT user_id) AS unique_users
            FROM pan_events {where}
            GROUP BY event_type
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<EventTypeUserRow>(sql, ct,
            persona: persona, teamId: teamId, from: from, to: to);
        return rows.Select(r => new EventTypeUserCountRow(
            EventType: (AnalyticsEventType)r.EventType,
            UniqueUsers: r.UniqueUsers)).ToList();
    }

    public async Task<IReadOnlyList<SessionEventTypeRow>> GetSessionEventTypesAsync(
        AnalyticsEventType[] eventTypes, string? persona, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        if (eventTypes.Length == 0) return [];
        var inClause = string.Join(",", eventTypes.Select(e => (int)e));
        var where = BuildWhereClause(persona: persona, from: from, to: to,
            extra: $"AND event_type IN ({inClause})");
        var sql = $"""
            SELECT session_id, event_type, min(occurred_at) AS first_occurrence
            FROM pan_events {where}
            GROUP BY session_id, event_type
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<SessionEventTypeRecord>(sql, ct,
            persona: persona, from: from, to: to);
        return rows.Select(r => new SessionEventTypeRow(
            SessionId: r.SessionId,
            EventType: (AnalyticsEventType)r.EventType,
            FirstOccurrence: r.FirstOccurrence)).ToList();
    }

    public async Task<int> CountDistinctSessionsAsync(
        string? persona, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var where = BuildWhereClause(persona: persona, from: from, to: to);
        return await ExecuteScalarAsync<int>(
            $"SELECT count(DISTINCT session_id) FROM pan_events {where}", ct,
            persona: persona, from: from, to: to);
    }

    public async Task<IReadOnlyList<UserFirstEventRow>> GetUserFirstEventTimesAsync(
        AnalyticsEventType[] eventTypes, string? persona, string? teamId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        if (eventTypes.Length == 0) return [];
        var inClause = string.Join(",", eventTypes.Select(e => (int)e));
        var where = BuildWhereClause(persona: persona, teamId: teamId, from: from, to: to,
            extra: $"AND event_type IN ({inClause})");
        var sql = $"""
            SELECT user_id, event_type, min(occurred_at) AS first_occurrence
            FROM pan_events {where}
            GROUP BY user_id, event_type
            FORMAT JSONEachRow
            """;
        var rows = await ExecuteQueryAsync<UserFirstRecord>(sql, ct,
            persona: persona, teamId: teamId, from: from, to: to);
        return rows.Select(r => new UserFirstEventRow(
            UserId: r.UserId,
            EventType: (AnalyticsEventType)r.EventType,
            FirstOccurrence: r.FirstOccurrence)).ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private string BuildWhereClause(
        string? persona = null,
        ProductModule? module = null,
        string? teamId = null,
        string? domainId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int? eventType = null,
        string? extra = null)
    {
        var conditions = new List<string>();

        // Defense-in-depth: always filter by tenant
        conditions.Add($"tenant_id = {currentTenant.Id}");

        if (!string.IsNullOrWhiteSpace(persona))
            conditions.Add($"persona = {{p_persona:String}}");
        if (module.HasValue)
            conditions.Add($"module = {(int)module.Value}");
        if (!string.IsNullOrWhiteSpace(teamId))
            conditions.Add($"team_id = {{p_team_id:String}}");
        if (!string.IsNullOrWhiteSpace(domainId))
            conditions.Add($"domain_id = {{p_domain_id:String}}");
        if (from.HasValue)
            conditions.Add($"occurred_at >= '{{p_from:DateTime}}'");
        if (to.HasValue)
            conditions.Add($"occurred_at <= '{{p_to:DateTime}}'");
        if (eventType.HasValue)
            conditions.Add($"event_type = {eventType.Value}");
        if (!string.IsNullOrWhiteSpace(extra))
            conditions.Add(extra);

        return conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;
    }

    private static string BuildQueryParameters(
        string? persona = null,
        string? teamId = null,
        string? domainId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(persona))
            parameters.Add($"param_p_persona={Uri.EscapeDataString(persona)}");
        if (!string.IsNullOrWhiteSpace(teamId))
            parameters.Add($"param_p_team_id={Uri.EscapeDataString(teamId)}");
        if (!string.IsNullOrWhiteSpace(domainId))
            parameters.Add($"param_p_domain_id={Uri.EscapeDataString(domainId)}");
        if (from.HasValue)
            parameters.Add($"param_p_from={Uri.EscapeDataString(from.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"))}");
        if (to.HasValue)
            parameters.Add($"param_p_to={Uri.EscapeDataString(to.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"))}");

        return parameters.Count > 0
            ? "&" + string.Join("&", parameters)
            : string.Empty;
    }

    private static ProductModule? ParseModule(string? module)
        => module is not null && Enum.TryParse<ProductModule>(module, true, out var m) ? m : null;

    private async Task<T> ExecuteScalarAsync<T>(
        string sql,
        CancellationToken ct,
        string? persona = null,
        string? teamId = null,
        string? domainId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null) where T : struct
    {
        try
        {
            var queryParams = BuildQueryParameters(persona, teamId, domainId, from, to);
            var url = $"{Endpoint}/?database=nextraceone_analytics&query={Uri.EscapeDataString(sql + " FORMAT JSONEachRow")}{queryParams}";
            var responseText = await httpClient.GetStringAsync(url, ct);
            var line = responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (line is null) return default;
            using var doc = JsonDocument.Parse(line);
            var prop = doc.RootElement.EnumerateObject().FirstOrDefault().Value;
            return prop.ValueKind == JsonValueKind.Number
                ? prop.Deserialize<T>()
                : default;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ClickHouse scalar query failed, returning default");
            return default;
        }
    }

    private async Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(
        string sql,
        CancellationToken ct,
        string? persona = null,
        string? teamId = null,
        string? domainId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
    {
        try
        {
            var queryParams = BuildQueryParameters(persona, teamId, domainId, from, to);
            var url = $"{Endpoint}/?database=nextraceone_analytics&query={Uri.EscapeDataString(sql)}{queryParams}";
            var responseText = await httpClient.GetStringAsync(url, ct);
            var results = new List<T>();
            foreach (var line in responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var item = JsonSerializer.Deserialize<T>(line, JsonOptions);
                if (item is not null) results.Add(item);
            }
            return results;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ClickHouse query failed, returning empty list");
            return [];
        }
    }

    // ── Private row DTOs ─────────────────────────────────────────────────────────

    private sealed record ModuleRow(int Module, long EventCount, int UniqueUsers);
    private sealed record FeatureCountRow(int Module, string Feature, long Cnt);
    private sealed record SessionEventRecord(string SessionId, int EventType, DateTimeOffset OccurredAt);
    private sealed record PersonaRow(string Persona, long EventCount, int UniqueUsers);
    private sealed record DistinctEventTypeRow(int EventType);
    private sealed record EventTypeCountRecord(int EventType, long Cnt);
    private sealed record EventTypeUserRow(int EventType, int UniqueUsers);
    private sealed record SessionEventTypeRecord(string SessionId, int EventType, DateTimeOffset FirstOccurrence);
    private sealed record UserFirstRecord(string UserId, int EventType, DateTimeOffset FirstOccurrence);
}

/// <summary>Configuration for ClickHouse analytics reads.</summary>
public sealed class ClickHouseAnalyticsOptions
{
    public const string SectionName = "ClickHouse:Analytics";
    public string Endpoint { get; set; } = "http://localhost:8123";
}
