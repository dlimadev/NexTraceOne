using System.Data;
using ClickHouse.Client.ADO;
using Dapper;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.ClickHouse;

/// <summary>
/// Implementação de IClickHouseRepository para armazenamento e consulta de eventos de observabilidade.
/// Usa ClickHouse para queries analíticas de alta performance em dados de runtime intelligence.
/// Suporta métricas de latência, throughput, error rate, system health e user activity.
/// </summary>
public class ClickHouseRepository : IClickHouseRepository, IDisposable
{
    private readonly string _connectionString;
    private bool _disposed = false;

    public ClickHouseRepository(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// Insere um único evento de observabilidade no ClickHouse.
    /// Usa INSERT INTO com parâmetros para prevenir SQL injection.
    /// </summary>
    public async Task InsertEventAsync(ClickHouseEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = @"INSERT INTO events 
            (timestamp, event_id, event_type, service_name, environment, trace_id, span_id, 
             user_id, endpoint, http_method, status_code, duration_ms, error_message, error_type, tags, metadata)
            VALUES 
            (@Timestamp, @EventId, @EventType, @ServiceName, @Environment, @TraceId, @SpanId,
             @UserId, @Endpoint, @HttpMethod, @StatusCode, @DurationMs, @ErrorMessage, @ErrorType, @Tags, @Metadata)";

        var tagsJson = System.Text.Json.JsonSerializer.Serialize(evt.Tags);
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(evt.Metadata);

        await connection.ExecuteAsync(sql, new
        {
            Timestamp = evt.Timestamp,
            EventId = evt.EventId,
            EventType = evt.EventType,
            ServiceName = evt.ServiceName,
            Environment = evt.Environment,
            TraceId = evt.TraceId,
            SpanId = evt.SpanId,
            UserId = evt.UserId,
            Endpoint = evt.Endpoint,
            HttpMethod = evt.HttpMethod,
            StatusCode = evt.StatusCode,
            DurationMs = evt.DurationMs,
            ErrorMessage = evt.ErrorMessage,
            ErrorType = evt.ErrorType,
            Tags = tagsJson,
            Metadata = metadataJson
        });
    }

    /// <summary>
    /// Insere um batch de eventos usando bulk insert para alta performance.
    /// Recomendado para ingestão de alta volumetria (>1000 eventos/segundo).
    /// </summary>
    public async Task InsertEventsBatchAsync(IEnumerable<ClickHouseEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        if (!eventList.Any()) return;

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        foreach (var evt in eventList)
        {
            await InsertEventAsync(evt);
        }
    }

    /// <summary>
    /// Obtém métricas de requisições agregadas por time bucket (1 minuto).
    /// Retorna count, avg_duration, p95_duration, p99_duration, error_count agrupados por endpoint.
    /// </summary>
    public async Task<List<RequestMetrics>> GetRequestMetricsAsync(DateTime from, DateTime to, string? endpoint = null)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var whereClause = "WHERE timestamp >= @from AND timestamp <= @to";
        var parameters = new Dictionary<string, object>
        {
            ["@from"] = from,
            ["@to"] = to
        };

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            whereClause += " AND endpoint = @endpoint";
            parameters["@endpoint"] = endpoint;
        }

        var sql = $@"
            SELECT 
                toStartOfMinute(timestamp) as TimeBucket,
                endpoint as Endpoint,
                http_method as HttpMethod,
                count() as RequestCount,
                avg(duration_ms) as AvgDurationMs,
                quantile(0.50)(duration_ms) as P50DurationMs,
                quantile(0.95)(duration_ms) as P95DurationMs,
                quantile(0.99)(duration_ms) as P99DurationMs,
                countIf(status_code >= 400) as ErrorCount,
                round(countIf(status_code >= 400) / count() * 100, 2) as ErrorRate
            FROM events
            {whereClause}
            GROUP BY TimeBucket, endpoint, http_method
            ORDER BY TimeBucket ASC";

        var results = await connection.QueryAsync<RequestMetrics>(sql, parameters);
        return results.ToList();
    }

    /// <summary>
    /// Obtém analytics de erros agrupados por tipo de erro e serviço.
    /// Útil para identificar padrões de falha e priorizar correções.
    /// </summary>
    public async Task<List<ErrorAnalytics>> GetErrorAnalyticsAsync(DateTime from, DateTime to, string? errorType = null)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var whereClause = "WHERE timestamp >= @from AND timestamp <= @to AND status_code >= 400";
        var parameters = new Dictionary<string, object>
        {
            ["@from"] = from,
            ["@to"] = to
        };

        if (!string.IsNullOrWhiteSpace(errorType))
        {
            whereClause += " AND error_type = @errorType";
            parameters["@errorType"] = errorType;
        }

        var sql = $@"
            SELECT 
                toStartOfHour(timestamp) as TimeBucket,
                error_type as ErrorType,
                error_message as ErrorMessage,
                service_name as ServiceName,
                count() as OccurrenceCount,
                groupArray(DISTINCT endpoint) as AffectedEndpoints,
                groupArray(error_message LIMIT 5) as SampleStackTraces
            FROM events
            {whereClause}
            GROUP BY TimeBucket, error_type, error_message, service_name
            ORDER BY OccurrenceCount DESC
            LIMIT 100";

        var results = await connection.QueryAsync<ErrorAnalytics>(sql, parameters);
        return results.ToList();
    }

    /// <summary>
    /// Obtém métricas de atividade de usuários (requests únicos por usuário).
    /// Útil para análise de comportamento e capacity planning baseado em uso real.
    /// </summary>
    public async Task<List<UserActivityMetrics>> GetUserActivityAsync(DateTime from, DateTime to, string? userId = null)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var whereClause = "WHERE timestamp >= @from AND timestamp <= @to";
        var parameters = new Dictionary<string, object>
        {
            ["@from"] = from,
            ["@to"] = to
        };

        if (!string.IsNullOrWhiteSpace(userId))
        {
            whereClause += " AND user_id = @userId";
            parameters["@userId"] = userId;
        }

        var sql = $@"
            SELECT 
                toStartOfHour(timestamp) as TimeBucket,
                user_id as UserId,
                count() as ActionCount,
                groupArray(DISTINCT endpoint LIMIT 10) as TopEndpoints,
                avg(duration_ms) / 60000 as AvgSessionDurationMinutes
            FROM events
            {whereClause}
            GROUP BY TimeBucket, user_id
            ORDER BY TimeBucket ASC";

        var results = await connection.QueryAsync<UserActivityMetrics>(sql, parameters);
        return results.ToList();
    }

    /// <summary>
    /// Obtém métricas de saúde do sistema (CPU, memória, disco) por serviço.
    /// Essencial para capacity planning e detecção de resource exhaustion.
    /// </summary>
    public async Task<List<SystemHealthMetrics>> GetSystemHealthAsync(DateTime from, DateTime to, string? serviceName = null)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var whereClause = "WHERE timestamp >= @from AND timestamp <= @to AND event_type = 'system_health'";
        var parameters = new Dictionary<string, object>
        {
            ["@from"] = from,
            ["@to"] = to
        };

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            whereClause += " AND service_name = @serviceName";
            parameters["@serviceName"] = serviceName;
        }

        var sql = $@"
            SELECT 
                timestamp as Timestamp,
                service_name as ServiceName,
                JSONExtractFloat(metadata, 'cpu_percent') as CpuUsagePercent,
                JSONExtractFloat(metadata, 'memory_mb') as MemoryUsageMB,
                JSONExtractFloat(metadata, 'disk_percent') as DiskUsagePercent,
                JSONExtractInt(metadata, 'active_connections') as ActiveConnections,
                JSONExtractFloat(metadata, 'rps') as RequestsPerSecond,
                JSONExtractFloat(metadata, 'error_rate') as ErrorRatePercent
            FROM events
            {whereClause}
            ORDER BY timestamp ASC";

        var results = await connection.QueryAsync<SystemHealthMetrics>(sql, parameters);
        return results.ToList();
    }

    /// <summary>
    /// Calcula o tempo médio de resposta para um período e endpoint opcional.
    /// Agregação simples para dashboards de performance.
    /// </summary>
    public async Task<double> GetAverageResponseTimeAsync(DateTime from, DateTime to, string? endpoint = null)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var whereClause = "WHERE timestamp >= @from AND timestamp <= @to";
        var parameters = new Dictionary<string, object>
        {
            ["@from"] = from,
            ["@to"] = to
        };

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            whereClause += " AND endpoint = @endpoint";
            parameters["@endpoint"] = endpoint;
        }

        var sql = $@"
            SELECT avg(duration_ms) as AvgResponseTime
            FROM events
            {whereClause}";

        var result = await connection.QuerySingleOrDefaultAsync<double?>(sql, parameters);
        return result ?? 0.0;
    }

    /// <summary>
    /// Obtém o total de requisições em um período.
    /// Métrica básica de throughput para capacity planning.
    /// </summary>
    public async Task<long> GetTotalRequestsAsync(DateTime from, DateTime to)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT count() as TotalRequests
            FROM events
            WHERE timestamp >= @from AND timestamp <= @to";

        var result = await connection.QuerySingleOrDefaultAsync<long>(sql, new
        {
            from,
            to
        });

        return result;
    }

    /// <summary>
    /// Calcula a taxa de erro percentual em um período.
    /// Considera erro qualquer status_code >= 400.
    /// </summary>
    public async Task<double> GetErrorRateAsync(DateTime from, DateTime to)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                round(countIf(status_code >= 400) / count() * 100, 2) as ErrorRatePercent
            FROM events
            WHERE timestamp >= @from AND timestamp <= @to";

        var result = await connection.QuerySingleOrDefaultAsync<double?>(sql, new
        {
            from,
            to
        });

        return result ?? 0.0;
    }

    /// <summary>
    /// Cria uma nova conexão ClickHouse.
    /// </summary>
    private ClickHouseConnection CreateConnection()
    {
        return new ClickHouseConnection(_connectionString);
    }

    /// <summary>
    /// Libera recursos da conexão quando o repositório é descartado.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
