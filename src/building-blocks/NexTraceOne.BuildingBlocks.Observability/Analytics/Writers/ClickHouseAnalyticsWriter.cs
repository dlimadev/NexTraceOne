using System.Globalization;
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
/// Implementação do IAnalyticsWriter para ClickHouse via HTTP interface.
///
/// Utiliza o protocolo HTTP nativo do ClickHouse (porta 8123) com formato JSONEachRow.
/// Endpoint padrão: POST http://clickhouse:8123/?query=INSERT INTO table FORMAT JSONEachRow
///
/// Princípios de implementação:
/// - Writes são append-only (INSERT apenas, nunca UPDATE/DELETE)
/// - Falhas são logadas mas não propagadas quando SuppressWriteErrors = true
/// - Batches de eventos são preferíveis para reduzir overhead de rede
/// - tenant_id é sempre incluído em cada registo para isolamento multi-tenant
///
/// NOTA: Esta implementação usa o HttpClient com timeout configurável.
/// A base de dados alvo é nextraceone_analytics (separada de nextraceone_obs).
/// </summary>
public sealed class ClickHouseAnalyticsWriter : IAnalyticsWriter, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly AnalyticsOptions _options;
    private readonly ILogger<ClickHouseAnalyticsWriter> _logger;
    private bool _disposed;

    public ClickHouseAnalyticsWriter(
        HttpClient httpClient,
        IOptions<AnalyticsOptions> options,
        ILogger<ClickHouseAnalyticsWriter> logger)
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

        var rows = records.Select(r => new
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
            occurred_at = FormatDateTime(r.OccurredAt),
            environment_id = r.EnvironmentId,
            duration_ms = r.DurationMs,
            parent_event_id = r.ParentEventId,
            source = r.Source
        });

        await InsertAsync("nextraceone_analytics.pan_events", rows, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteRuntimeMetricAsync(RuntimeMetricRecord record, CancellationToken cancellationToken = default)
    {
        var row = new
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
            captured_at = FormatDateTime(record.CapturedAt)
        };

        await InsertAsync("nextraceone_analytics.ops_runtime_metrics", [row], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteCostEntryAsync(CostEntryRecord record, CancellationToken cancellationToken = default)
    {
        var row = new
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
            captured_at = FormatDateTime(record.CapturedAt)
        };

        await InsertAsync("nextraceone_analytics.ops_cost_entries", [row], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteIncidentTrendEventAsync(IncidentTrendRecord record, CancellationToken cancellationToken = default)
    {
        var row = new
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
            change_correlated = record.ChangeCorrelated ? 1 : 0,
            mttr_minutes = record.MttrMinutes,
            occurred_at = FormatDateTime(record.OccurredAt)
        };

        await InsertAsync("nextraceone_analytics.ops_incident_trends", [row], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteIntegrationExecutionAsync(IntegrationExecutionRecord record, CancellationToken cancellationToken = default)
    {
        var row = new
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
            started_at = FormatDateTime(record.StartedAt),
            completed_at = record.CompletedAt.HasValue ? FormatDateTime(record.CompletedAt.Value) : null,
            duration_ms = record.DurationMs,
            result = record.Result,
            items_processed = record.ItemsProcessed,
            items_succeeded = record.ItemsSucceeded,
            items_failed = record.ItemsFailed,
            error_code = record.ErrorCode,
            retry_attempt = record.RetryAttempt,
            created_at = FormatDateTime(record.CreatedAt)
        };

        await InsertAsync("nextraceone_analytics.int_execution_logs", [row], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteConnectorHealthEventAsync(ConnectorHealthRecord record, CancellationToken cancellationToken = default)
    {
        var row = new
        {
            tenant_id = record.TenantId,
            connector_id = record.ConnectorId,
            connector_name = record.ConnectorName,
            health = record.Health,
            previous_health = record.PreviousHealth,
            freshness_lag_minutes = record.FreshnessLagMinutes,
            changed_at = FormatDateTime(record.ChangedAt)
        };

        await InsertAsync("nextraceone_analytics.int_health_history", [row], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteComplianceTrendAsync(ComplianceTrendRecord record, CancellationToken cancellationToken = default)
    {
        var row = new
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
            captured_at = FormatDateTime(record.CapturedAt)
        };

        await InsertAsync("nextraceone_analytics.gov_compliance_trends", [row], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteFinOpsAggregateAsync(FinOpsAggregateRecord record, CancellationToken cancellationToken = default)
    {
        var row = new
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
            anomaly_detected = record.AnomalyDetected ? 1 : 0,
            captured_at = FormatDateTime(record.CapturedAt)
        };

        await InsertAsync("nextraceone_analytics.gov_finops_aggregates", [row], cancellationToken);
    }

    /// <summary>
    /// Executa um INSERT em formato JSONEachRow via HTTP interface do ClickHouse.
    /// </summary>
    private async Task InsertAsync<T>(string table, IEnumerable<T> rows, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.WriteTimeoutSeconds));

        try
        {
            var jsonLines = string.Join('\n', rows.Select(r => JsonSerializer.Serialize(r, JsonOptions)));
            var query = Uri.EscapeDataString($"INSERT INTO {table} FORMAT JSONEachRow");
            var requestUri = new Uri(_options.ConnectionString.TrimEnd('/') + $"/?query={query}");

            using var content = new StringContent(jsonLines, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(requestUri, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cts.Token);
                throw new InvalidOperationException(
                    $"ClickHouse INSERT into {table} failed: HTTP {(int)response.StatusCode} — {body}");
            }
        }
        catch (Exception ex) when (_options.SuppressWriteErrors)
        {
            _logger.LogWarning(ex,
                "ClickHouse analytics write suppressed for table {Table}. Error: {Message}",
                table, ex.Message);
        }
    }

    /// <summary>
    /// Formata um DateTimeOffset no formato aceite pelo ClickHouse DateTime64 UTC.
    /// Exemplo: "2026-03-25 21:00:00.000"
    /// </summary>
    private static string FormatDateTime(DateTimeOffset dt)
        => dt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
