using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Repositories;

/// <summary>
/// Elasticsearch-backed implementation of <see cref="IAnalyticsEventRepository"/>.
/// Reads from the same index written by <see cref="ElasticAnalyticsWriter"/>
/// (default index pattern: {IndexPrefix}-pan-events).
/// Writes (AddAsync) delegate to the PostgreSQL fallback repository.
/// Activated when Telemetry:ObservabilityProvider:Provider = "Elastic" (the default).
/// </summary>
internal sealed class ElasticsearchAnalyticsEventRepository(
    HttpClient httpClient,
    IOptions<AnalyticsOptions> options,
    IAnalyticsEventRepository fallbackRepository,
    ICurrentTenant currentTenant,
    ILogger<ElasticsearchAnalyticsEventRepository> logger) : IAnalyticsEventRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private readonly AnalyticsOptions _opts = options.Value;

    private string IndexName => $"{_opts.IndexPrefix}-pan-events";
    private string Endpoint => _opts.ConnectionString.TrimEnd('/');

    // AddAsync delegates to the PostgreSQL fallback repository.
    public async Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken ct)
        => await fallbackRepository.AddAsync(analyticsEvent, ct);

    public async Task<long> CountAsync(
        string? persona, ProductModule? module, string? teamId, string? domainId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, module: module, teamId: teamId, domainId: domainId, from: from, to: to);
        var response = await SearchAsync(body, ct);
        return response.Hits?.Total?.Value ?? 0;
    }

    public async Task<long> CountByEventTypeAsync(
        AnalyticsEventType eventType, string? persona, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, eventType: (int)eventType, from: from, to: to);
        var response = await SearchAsync(body, ct);
        return response.Hits?.Total?.Value ?? 0;
    }

    public async Task<int> CountUniqueUsersAsync(
        string? persona, ProductModule? module, string? teamId, string? domainId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, module: module, teamId: teamId, domainId: domainId, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["unique_users"] = new { cardinality = new { field = "user_id" } }
        };

        var response = await SearchAsync(body, ct);
        if (response.Aggregations is not null
            && response.Aggregations.TryGetValue("unique_users", out var agg)
            && agg is JsonElement el
            && el.TryGetProperty("value", out var val))
        {
            return val.GetInt32();
        }

        return 0;
    }

    public async Task<int> CountActivePersonasAsync(
        string? module, string? teamId, string? domainId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(module: ParseModule(module), teamId: teamId, domainId: domainId, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["active_personas"] = new { cardinality = new { field = "persona" } }
        };

        var response = await SearchAsync(body, ct);
        if (response.Aggregations is not null
            && response.Aggregations.TryGetValue("active_personas", out var agg)
            && agg is JsonElement el
            && el.TryGetProperty("value", out var val))
        {
            return val.GetInt32();
        }

        return 0;
    }

    public async Task<IReadOnlyList<ModuleUsageRow>> GetTopModulesAsync(
        string? persona, string? teamId, string? domainId,
        DateTimeOffset from, DateTimeOffset to, int top, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, teamId: teamId, domainId: domainId, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["modules"] = new
            {
                terms = new { field = "module", size = top, order = new { _count = "desc" } },
                aggs = new
                {
                    unique_users = new { cardinality = new { field = "user_id" } }
                }
            }
        };

        var response = await SearchAsync(body, ct);
        var buckets = GetBuckets(response, "modules");
        return buckets.Select(b => new ModuleUsageRow(
            Module: Enum.Parse<ProductModule>(b.Key!, true),
            EventCount: b.DocCount,
            UniqueUsers: GetSubAggInt(b, "unique_users", "value"))).ToList();
    }

    public async Task<IReadOnlyList<ModuleAdoptionRow>> GetModuleAdoptionAsync(
        string? persona, string? teamId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, teamId: teamId, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["modules"] = new
            {
                terms = new { field = "module", size = 100, order = new { _count = "desc" } },
                aggs = new
                {
                    unique_users = new { cardinality = new { field = "user_id" } }
                }
            }
        };

        var response = await SearchAsync(body, ct);
        var buckets = GetBuckets(response, "modules");
        return buckets.Select(b => new ModuleAdoptionRow(
            Module: Enum.Parse<ProductModule>(b.Key!, true),
            TotalActions: b.DocCount,
            UniqueUsers: GetSubAggInt(b, "unique_users", "value"))).ToList();
    }

    public async Task<IReadOnlyList<ModuleFeatureCountRow>> GetFeatureCountsAsync(
        string? persona, string? teamId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, teamId: teamId, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["features"] = new
            {
                composite = new
                {
                    size = 1000,
                    sources = new List<object>
                    {
                        new Dictionary<string, object> { ["module"] = new { terms = new { field = "module" } } },
                        new Dictionary<string, object> { ["feature"] = new { terms = new { field = "feature" } } }
                    }
                }
            }
        };

        var response = await SearchAsync(body, ct);
        var buckets = GetBuckets(response, "features");
        return buckets.Select(b => new ModuleFeatureCountRow(
            Module: Enum.Parse<ProductModule>(b.KeyModule!, true),
            Feature: b.KeyFeature!,
            Count: b.DocCount)).ToList();
    }

    public async Task<IReadOnlyList<SessionEventRow>> ListSessionEventsAsync(
        string? persona, string? teamId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, teamId: teamId, from: from, to: to);
        body.Size = 10_000;
        body.Sort = new[] { new { occurred_at = new { order = "asc" } } };

        var response = await SearchAsync(body, ct);
        return (response.Hits?.Hits ?? [])
            .Select(h => new SessionEventRow(
                SessionId: h.Source.TryGetProperty("session_id", out var sid) ? sid.GetString() ?? string.Empty : string.Empty,
                EventType: h.Source.TryGetProperty("event_type", out var et)
                    ? (AnalyticsEventType)et.GetInt32()
                    : AnalyticsEventType.ModuleViewed,
                OccurredAt: h.Source.TryGetProperty("occurred_at", out var oa)
                    ? oa.GetDateTimeOffset()
                    : DateTimeOffset.MinValue))
            .ToList();
    }

    public async Task<IReadOnlyList<PersonaBreakdownRow>> GetPersonaBreakdownAsync(
        string? teamId, string? domainId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(teamId: teamId, domainId: domainId, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["personas"] = new
            {
                terms = new { field = "persona", size = 100, order = new { _count = "desc" } },
                aggs = new
                {
                    unique_users = new { cardinality = new { field = "user_id" } }
                }
            }
        };

        var response = await SearchAsync(body, ct);
        var buckets = GetBuckets(response, "personas");
        return buckets.Select(b => new PersonaBreakdownRow(
            Persona: b.Key!,
            EventCount: b.DocCount,
            UniqueUsers: GetSubAggInt(b, "unique_users", "value"))).ToList();
    }

    public async Task<IReadOnlyList<EventTypeCountRow>> GetTopEventTypesAsync(
        string? persona, DateTimeOffset from, DateTimeOffset to, int top, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["event_types"] = new
            {
                terms = new { field = "event_type", size = top, order = new { _count = "desc" } }
            }
        };

        var response = await SearchAsync(body, ct);
        var buckets = GetBuckets(response, "event_types");
        return buckets.Select(b => new EventTypeCountRow(
            EventType: (AnalyticsEventType)int.Parse(b.Key!),
            Count: b.DocCount)).ToList();
    }

    public async Task<IReadOnlyList<AnalyticsEventType>> GetDistinctEventTypesAsync(
        string? persona, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["event_types"] = new
            {
                terms = new { field = "event_type", size = 100 }
            }
        };

        var response = await SearchAsync(body, ct);
        var buckets = GetBuckets(response, "event_types");
        return buckets.Select(b => (AnalyticsEventType)int.Parse(b.Key!)).ToList();
    }

    public async Task<IReadOnlyList<EventTypeUserCountRow>> CountUsersByEventTypeAsync(
        AnalyticsEventType[] eventTypes, string? persona, string? teamId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        if (eventTypes.Length == 0) return [];

        var body = BuildQuery(persona: persona, teamId: teamId, from: from, to: to);
        body.PostFilter = new
        {
            terms = new Dictionary<string, object> { ["event_type"] = eventTypes.Select(e => (int)e).ToList() }
        };
        body.Aggs = new Dictionary<string, object>
        {
            ["event_types"] = new
            {
                terms = new { field = "event_type", size = eventTypes.Length, min_doc_count = 1 },
                aggs = new
                {
                    unique_users = new { cardinality = new { field = "user_id" } }
                }
            }
        };

        var response = await SearchAsync(body, ct);
        var buckets = GetBuckets(response, "event_types");
        return buckets.Select(b => new EventTypeUserCountRow(
            EventType: (AnalyticsEventType)int.Parse(b.Key!),
            UniqueUsers: GetSubAggInt(b, "unique_users", "value"))).ToList();
    }

    public async Task<IReadOnlyList<SessionEventTypeRow>> GetSessionEventTypesAsync(
        AnalyticsEventType[] eventTypes, string? persona, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        if (eventTypes.Length == 0) return [];

        var body = BuildQuery(persona: persona, from: from, to: to);
        body.PostFilter = new
        {
            terms = new Dictionary<string, object> { ["event_type"] = eventTypes.Select(e => (int)e).ToList() }
        };
        body.Aggs = new Dictionary<string, object>
        {
            ["sessions"] = new
            {
                terms = new { field = "session_id", size = 10_000 },
                aggs = new
                {
                    first_occurrence = new { min = new { field = "occurred_at" } },
                    event_types = new
                    {
                        terms = new { field = "event_type", size = eventTypes.Length }
                    }
                }
            }
        };

        var response = await SearchAsync(body, ct);
        var buckets = GetBuckets(response, "sessions");
        var rows = new List<SessionEventTypeRow>();

        foreach (var sessionBucket in buckets)
        {
            var sessionId = sessionBucket.Key!;
            var firstOccurrence = GetSubAggDate(sessionBucket, "first_occurrence", "value");
            var etBuckets = GetSubBuckets(sessionBucket, "event_types");
            foreach (var et in etBuckets)
            {
                rows.Add(new SessionEventTypeRow(
                    SessionId: sessionId,
                    EventType: (AnalyticsEventType)int.Parse(et.Key!),
                    FirstOccurrence: firstOccurrence));
            }
        }

        return rows;
    }

    public async Task<int> CountDistinctSessionsAsync(
        string? persona, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var body = BuildQuery(persona: persona, from: from, to: to);
        body.Aggs = new Dictionary<string, object>
        {
            ["distinct_sessions"] = new { cardinality = new { field = "session_id" } }
        };

        var response = await SearchAsync(body, ct);
        if (response.Aggregations is not null
            && response.Aggregations.TryGetValue("distinct_sessions", out var agg)
            && agg is JsonElement el
            && el.TryGetProperty("value", out var val))
        {
            return val.GetInt32();
        }

        return 0;
    }

    public async Task<IReadOnlyList<UserFirstEventRow>> GetUserFirstEventTimesAsync(
        AnalyticsEventType[] eventTypes, string? persona, string? teamId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        if (eventTypes.Length == 0) return [];

        var body = BuildQuery(persona: persona, teamId: teamId, from: from, to: to);
        body.PostFilter = new
        {
            terms = new Dictionary<string, object> { ["event_type"] = eventTypes.Select(e => (int)e).ToList() }
        };
        body.Aggs = new Dictionary<string, object>
        {
            ["users"] = new
            {
                terms = new { field = "user_id", size = 10_000 },
                aggs = new
                {
                    event_types = new
                    {
                        terms = new { field = "event_type", size = eventTypes.Length },
                        aggs = new
                        {
                            first_occurrence = new { min = new { field = "occurred_at" } }
                        }
                    }
                }
            }
        };

        var response = await SearchAsync(body, ct);
        var userBuckets = GetBuckets(response, "users");
        var rows = new List<UserFirstEventRow>();

        foreach (var userBucket in userBuckets)
        {
            var userId = userBucket.Key!;
            var etBuckets = GetSubBuckets(userBucket, "event_types");
            foreach (var et in etBuckets)
            {
                var firstOccurrence = GetSubAggDate(et, "first_occurrence", "value");
                rows.Add(new UserFirstEventRow(
                    UserId: userId,
                    EventType: (AnalyticsEventType)int.Parse(et.Key!),
                    FirstOccurrence: firstOccurrence));
            }
        }

        return rows;
    }

    // ── Query DSL construction ───────────────────────────────────────────────────

    private SearchRequest BuildQuery(
        string? persona = null,
        ProductModule? module = null,
        string? teamId = null,
        string? domainId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int? eventType = null)
    {
        var must = new List<object>
        {
            new { term = new Dictionary<string, object> { ["tenant_id"] = currentTenant.Id.ToString() } }
        };

        if (from.HasValue || to.HasValue)
        {
            var rangeSpec = new Dictionary<string, object>();
            if (from.HasValue) rangeSpec["gte"] = from.Value.ToString("O");
            if (to.HasValue) rangeSpec["lte"] = to.Value.ToString("O");
            must.Add(new { range = new Dictionary<string, object> { ["occurred_at"] = rangeSpec } });
        }

        if (!string.IsNullOrWhiteSpace(persona))
            must.Add(new { term = new Dictionary<string, object> { ["persona"] = persona } });

        if (module.HasValue)
            must.Add(new { term = new Dictionary<string, object> { ["module"] = module.Value.ToString() } });

        if (!string.IsNullOrWhiteSpace(teamId))
            must.Add(new { term = new Dictionary<string, object> { ["team_id"] = teamId } });

        if (!string.IsNullOrWhiteSpace(domainId))
            must.Add(new { term = new Dictionary<string, object> { ["domain_id"] = domainId } });

        if (eventType.HasValue)
            must.Add(new { term = new Dictionary<string, object> { ["event_type"] = eventType.Value } });

        return new SearchRequest
        {
            Query = new { @bool = new { must = must } },
            Size = 0,
        };
    }

    // ── HTTP execution ───────────────────────────────────────────────────────────

    private async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        if (!_opts.Enabled || string.IsNullOrWhiteSpace(_opts.ConnectionString))
        {
            logger.LogDebug("Elasticsearch analytics disabled or endpoint not configured — returning empty result.");
            return new SearchResponse();
        }

        try
        {
            var url = $"{Endpoint}/{IndexName}/_search";
            var json = JsonSerializer.Serialize(request, JsonOptions);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(_opts.ApiKey))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", _opts.ApiKey);

            var response = await httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Elasticsearch analytics query returned {Status}.", response.StatusCode);
                return new SearchResponse();
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<SearchResponse>(responseJson, JsonOptions) ?? new SearchResponse();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Elasticsearch analytics query failed — returning empty result.");
            return new SearchResponse();
        }
    }

    // ── Aggregation parsing helpers ──────────────────────────────────────────────

    private static List<AggregationBucket> GetBuckets(SearchResponse response, string aggName)
    {
        if (response.Aggregations is null) return [];
        if (!response.Aggregations.TryGetValue(aggName, out var agg)) return [];
        if (agg is not JsonElement el) return [];
        if (!el.TryGetProperty("buckets", out var buckets)) return [];

        var list = new List<AggregationBucket>();
        foreach (var b in buckets.EnumerateArray())
        {
            list.Add(new AggregationBucket(b));
        }
        return list;
    }

    private static List<AggregationBucket> GetSubBuckets(AggregationBucket parent, string aggName)
    {
        if (!parent.Raw.TryGetProperty(aggName, out var agg)) return [];
        if (!agg.TryGetProperty("buckets", out var buckets)) return [];

        var list = new List<AggregationBucket>();
        foreach (var b in buckets.EnumerateArray())
        {
            list.Add(new AggregationBucket(b));
        }
        return list;
    }

    private static int GetSubAggInt(AggregationBucket bucket, string aggName, string property)
    {
        if (!bucket.Raw.TryGetProperty(aggName, out var agg)) return 0;
        if (!agg.TryGetProperty(property, out var prop)) return 0;
        return prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : 0;
    }

    private static DateTimeOffset GetSubAggDate(AggregationBucket bucket, string aggName, string property)
    {
        if (!bucket.Raw.TryGetProperty(aggName, out var agg)) return DateTimeOffset.MinValue;
        if (!agg.TryGetProperty(property, out var prop)) return DateTimeOffset.MinValue;
        return prop.ValueKind == JsonValueKind.String
            ? DateTimeOffset.Parse(prop.GetString()!)
            : DateTimeOffset.MinValue;
    }

    private static ProductModule? ParseModule(string? module)
        => module is not null && Enum.TryParse<ProductModule>(module, true, out var m) ? m : null;

    // ── DTOs ─────────────────────────────────────────────────────────────────────

    private sealed class SearchRequest
    {
        public object? Query { get; set; }
        public int? Size { get; set; }
        public object? Sort { get; set; }
        public object? Aggs { get; set; }
        public object? PostFilter { get; set; }
    }

    private sealed class SearchResponse
    {
        public HitsWrapper? Hits { get; set; }
        public Dictionary<string, object>? Aggregations { get; set; }
    }

    private sealed class HitsWrapper
    {
        public TotalWrapper? Total { get; set; }
        public List<Hit>? Hits { get; set; }
    }

    private sealed class TotalWrapper
    {
        public long Value { get; set; }
    }

    private sealed class Hit
    {
        public JsonElement _source { get; set; }
        public JsonElement Source => _source;
    }

    private sealed class AggregationBucket
    {
        public JsonElement Raw { get; }
        public string? Key { get; }
        public string? KeyModule { get; }
        public string? KeyFeature { get; }
        public long DocCount { get; }

        public AggregationBucket(JsonElement element)
        {
            Raw = element;
            DocCount = element.TryGetProperty("doc_count", out var dc) ? dc.GetInt64() : 0;

            if (element.TryGetProperty("key", out var keyProp))
            {
                if (keyProp.ValueKind == JsonValueKind.String)
                {
                    Key = keyProp.GetString();
                }
                else if (keyProp.ValueKind == JsonValueKind.Number)
                {
                    Key = keyProp.GetInt32().ToString();
                }
            }

            if (element.TryGetProperty("key", out var compositeKey) && compositeKey.ValueKind == JsonValueKind.Object)
            {
                if (compositeKey.TryGetProperty("module", out var mod))
                    KeyModule = mod.GetString();
                if (compositeKey.TryGetProperty("feature", out var feat))
                    KeyFeature = feat.GetString();
            }
        }
    }
}
