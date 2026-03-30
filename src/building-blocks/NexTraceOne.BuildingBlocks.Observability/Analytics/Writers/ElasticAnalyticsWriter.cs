using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Events;

namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Writers;

/// <summary>
/// Implementação do IAnalyticsWriter para Elasticsearch via Bulk API.
///
/// Utiliza a Bulk API nativa do Elasticsearch (POST /_bulk) com formato NDJSON.
/// Endpoint padrão: POST http://elasticsearch:9200/_bulk
///
/// Princípios de implementação:
/// - Writes são append-only (index apenas, nunca update/delete)
/// - Falhas são logadas mas não propagadas quando SuppressWriteErrors = true
/// - Batches de eventos são preferíveis para reduzir overhead de rede
/// - tenant_id é sempre incluído em cada documento para isolamento multi-tenant
/// - Datas são formatadas em ISO 8601 (formato nativo do Elasticsearch)
///
/// NOTA: Esta implementação usa o HttpClient com timeout configurável.
/// Os índices alvo seguem o prefixo configurável (padrão: nextraceone-analytics).
/// </summary>
public sealed class ElasticAnalyticsWriter : IAnalyticsWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly AnalyticsOptions _options;
    private readonly ILogger<ElasticAnalyticsWriter> _logger;

    public ElasticAnalyticsWriter(
        HttpClient httpClient,
        IOptions<AnalyticsOptions> options,
        ILogger<ElasticAnalyticsWriter> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task WriteProductEventAsync(ProductAnalyticsRecord record, CancellationToken cancellationToken = default)
        => WriteProductEventsBatchAsync([record], cancellationToken);

    /// <inheritdoc />
    public async Task WriteProductEventsBatchAsync(IReadOnlyList<ProductAnalyticsRecord> records, CancellationToken cancellationToken = default)
    {
        if (records.Count == 0) return;

        if (records.Count > _options.MaxBatchSize)
        {
            for (var i = 0; i < records.Count; i += _options.MaxBatchSize)
            {
                var batch = records.Skip(i).Take(_options.MaxBatchSize).ToList();
                await WriteProductEventsBatchAsync(batch, cancellationToken);
            }
            return;
        }

        var documents = records.Select(r => (object)new
        {
            id = r.Id,
            tenant_id = r.TenantId,
            user_id = r.UserId,
            persona = r.Persona,
            module = r.Module,
            event_type = r.EventType,
            feature = r.Feature,
            entity_type = r.EntityType,
            outcome = r.Outcome,
            route = r.Route,
            team_id = r.TeamId,
            domain_id = r.DomainId,
            session_id = r.SessionId,
            client_type = r.ClientType,
            metadata_json = r.MetadataJson,
            occurred_at = r.OccurredAt,
            environment_id = r.EnvironmentId,
            duration_ms = r.DurationMs,
            parent_event_id = r.ParentEventId,
            source = r.Source
        });

        await BulkIndexAsync(ResolveIndexName("pan-events"), documents, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteRuntimeMetricAsync(RuntimeMetricRecord record, CancellationToken cancellationToken = default)
    {
        var doc = new
        {
            id = record.Id,
            tenant_id = record.TenantId,
            service_name = record.ServiceName,
            service_id = record.ServiceId,
            environment = record.Environment,
            environment_id = record.EnvironmentId,
            source = record.Source,
            avg_latency_ms = record.AvgLatencyMs,
            p99_latency_ms = record.P99LatencyMs,
            error_rate = record.ErrorRate,
            requests_per_second = record.RequestsPerSecond,
            cpu_usage_percent = record.CpuUsagePercent,
            memory_usage_mb = record.MemoryUsageMb,
            active_instances = record.ActiveInstances,
            health_status = record.HealthStatus,
            captured_at = record.CapturedAt
        };

        await BulkIndexAsync(ResolveIndexName("ops-runtime-metrics"), [(object)doc], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteCostEntryAsync(CostEntryRecord record, CancellationToken cancellationToken = default)
    {
        var doc = new
        {
            id = record.Id,
            tenant_id = record.TenantId,
            service_name = record.ServiceName,
            service_id = record.ServiceId,
            environment = record.Environment,
            environment_id = record.EnvironmentId,
            currency = record.Currency,
            period = record.Period,
            source = record.Source,
            total_cost = record.TotalCost,
            cpu_cost_share = record.CpuCostShare,
            memory_cost_share = record.MemoryCostShare,
            network_cost_share = record.NetworkCostShare,
            storage_cost_share = record.StorageCostShare,
            captured_at = record.CapturedAt
        };

        await BulkIndexAsync(ResolveIndexName("ops-cost-entries"), [(object)doc], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteIncidentTrendEventAsync(IncidentTrendRecord record, CancellationToken cancellationToken = default)
    {
        var doc = new
        {
            event_id = record.EventId,
            incident_id = record.IncidentId,
            tenant_id = record.TenantId,
            service_name = record.ServiceName,
            service_id = record.ServiceId,
            environment = record.Environment,
            environment_id = record.EnvironmentId,
            severity = record.Severity,
            incident_type = record.IncidentType,
            lifecycle_event = record.LifecycleEvent,
            change_correlated = record.ChangeCorrelated,
            mttr_minutes = record.MttrMinutes,
            occurred_at = record.OccurredAt
        };

        await BulkIndexAsync(ResolveIndexName("ops-incident-trends"), [(object)doc], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteIntegrationExecutionAsync(IntegrationExecutionRecord record, CancellationToken cancellationToken = default)
    {
        var doc = new
        {
            id = record.Id,
            tenant_id = record.TenantId,
            connector_id = record.ConnectorId,
            connector_name = record.ConnectorName,
            connector_type = record.ConnectorType,
            provider = record.Provider,
            source_id = record.SourceId,
            data_domain = record.DataDomain,
            correlation_id = record.CorrelationId,
            started_at = record.StartedAt,
            completed_at = record.CompletedAt,
            duration_ms = record.DurationMs,
            result = record.Result,
            items_processed = record.ItemsProcessed,
            items_succeeded = record.ItemsSucceeded,
            items_failed = record.ItemsFailed,
            error_code = record.ErrorCode,
            retry_attempt = record.RetryAttempt,
            created_at = record.CreatedAt
        };

        await BulkIndexAsync(ResolveIndexName("int-execution-logs"), [(object)doc], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteConnectorHealthEventAsync(ConnectorHealthRecord record, CancellationToken cancellationToken = default)
    {
        var doc = new
        {
            tenant_id = record.TenantId,
            connector_id = record.ConnectorId,
            connector_name = record.ConnectorName,
            health = record.Health,
            previous_health = record.PreviousHealth,
            freshness_lag_minutes = record.FreshnessLagMinutes,
            changed_at = record.ChangedAt
        };

        await BulkIndexAsync(ResolveIndexName("int-health-history"), [(object)doc], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteComplianceTrendAsync(ComplianceTrendRecord record, CancellationToken cancellationToken = default)
    {
        var doc = new
        {
            tenant_id = record.TenantId,
            service_id = record.ServiceId,
            service_name = record.ServiceName,
            policy_id = record.PolicyId,
            policy_name = record.PolicyName,
            environment = record.Environment,
            compliance_score = record.ComplianceScore,
            status = record.Status,
            violations_count = record.ViolationsCount,
            captured_at = record.CapturedAt
        };

        await BulkIndexAsync(ResolveIndexName("gov-compliance-trends"), [(object)doc], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteFinOpsAggregateAsync(FinOpsAggregateRecord record, CancellationToken cancellationToken = default)
    {
        var doc = new
        {
            tenant_id = record.TenantId,
            team_id = record.TeamId,
            team_name = record.TeamName,
            domain_name = record.DomainName,
            service_name = record.ServiceName,
            service_id = record.ServiceId,
            environment = record.Environment,
            currency = record.Currency,
            period_label = record.PeriodLabel,
            total_cost = record.TotalCost,
            compute_cost = record.ComputeCost,
            storage_cost = record.StorageCost,
            network_cost = record.NetworkCost,
            anomaly_detected = record.AnomalyDetected,
            captured_at = record.CapturedAt
        };

        await BulkIndexAsync(ResolveIndexName("gov-finops-aggregates"), [(object)doc], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteTraceReleaseMappingAsync(TraceReleaseMappingRecord record, CancellationToken cancellationToken = default)
    {
        var doc = new
        {
            id = record.Id,
            tenant_id = record.TenantId,
            release_id = record.ReleaseId,
            trace_id = record.TraceId,
            service_name = record.ServiceName,
            service_id = record.ServiceId,
            environment = record.Environment,
            environment_id = record.EnvironmentId,
            correlation_source = record.CorrelationSource,
            trace_started_at = record.TraceStartedAt,
            trace_ended_at = record.TraceEndedAt,
            correlated_at = record.CorrelatedAt
        };

        await BulkIndexAsync(ResolveIndexName("chg-trace-release-mapping"), [(object)doc], cancellationToken);
    }

    /// <summary>
    /// Resolve o nome completo do índice Elasticsearch com base no prefixo configurado.
    /// Exemplo: prefixo "nextraceone-analytics" + sufixo "pan-events" → "nextraceone-analytics-pan-events".
    /// </summary>
    private string ResolveIndexName(string suffix)
        => $"{_options.IndexPrefix}-{suffix}";

    /// <summary>
    /// Executa um bulk index via Elasticsearch Bulk API (POST /_bulk).
    /// Formato NDJSON: {"index":{"_index":"index-name"}}\n{...documento json...}\n
    /// O timeout é aplicado via HttpClient.Timeout configurado no DI.
    /// </summary>
    private async Task BulkIndexAsync(string indexName, IEnumerable<object> documents, CancellationToken cancellationToken)
    {
        try
        {
            var sb = new StringBuilder();
            var actionLine = JsonSerializer.Serialize(new { index = new { _index = indexName } }, JsonOptions);

            foreach (var doc in documents)
            {
                sb.AppendLine(actionLine);
                sb.AppendLine(JsonSerializer.Serialize(doc, JsonOptions));
            }

            var endpoint = _options.ConnectionString.TrimEnd('/');
            var requestUri = new Uri($"{endpoint}/_bulk");

            using var content = new StringContent(sb.ToString(), Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-ndjson");

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = content;

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("ApiKey", _options.ApiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var sanitizedBody = body.Length > 200 ? body[..200] + " [truncated]" : body;
                throw new InvalidOperationException(
                    $"Elasticsearch bulk index into {indexName} failed: HTTP {(int)response.StatusCode} — {sanitizedBody}");
            }
        }
        catch (Exception ex) when (_options.SuppressWriteErrors)
        {
            _logger.LogWarning(ex,
                "Elasticsearch analytics write suppressed for index {Index}. Error: {Message}",
                indexName, ex.Message);
        }
    }
}
