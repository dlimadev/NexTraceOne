using System.Data;
using ClickHouse.Client.ADO;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.SearchLogs;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação de <see cref="ITelemetrySearchService"/> sobre ClickHouse.
/// Usa ClickHouse para pesquisa de logs estruturados com alta performance.
/// Alternativa ao Elasticsearch para ambientes que preferem ClickHouse como backend de telemetria.
/// SaaS-07: Log Search UI.
/// </summary>
internal sealed class ClickHouseLogSearchService : ITelemetrySearchService, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<ClickHouseLogSearchService> _logger;
    private bool _disposed = false;

    public ClickHouseLogSearchService(
        IOptions<TelemetryStoreOptions> options,
        ILogger<ClickHouseLogSearchService> logger)
    {
        var clickHouseOptions = options.Value.ObservabilityProvider.ClickHouse;
        
        if (string.IsNullOrWhiteSpace(clickHouseOptions.ConnectionString))
        {
            throw new InvalidOperationException("ClickHouse connection string não configurada.");
        }

        _connectionString = clickHouseOptions.ConnectionString;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<SearchLogs.LogEntry> Entries, long Total)> SearchAsync(
        LogSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var whereClauses = new List<string>
            {
                "timestamp >= @From",
                "timestamp <= @To"
            };

            var parameters = new Dictionary<string, object>
            {
                ["@From"] = request.From.UtcDateTime,
                ["@To"] = request.To.UtcDateTime
            };

            if (!string.IsNullOrWhiteSpace(request.ServiceName))
            {
                whereClauses.Add("service_name = @ServiceName");
                parameters["@ServiceName"] = request.ServiceName;
            }

            if (!string.IsNullOrWhiteSpace(request.Severity))
            {
                whereClauses.Add("severity = @Severity");
                parameters["@Severity"] = request.Severity.ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(request.Environment))
            {
                whereClauses.Add("environment = @Environment");
                parameters["@Environment"] = request.Environment;
            }

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                whereClauses.Add("message ILIKE @SearchText");
                parameters["@SearchText"] = $"%{request.SearchText}%";
            }

            var whereClause = string.Join(" AND ", whereClauses);
            var offset = (request.Page - 1) * request.PageSize;

            var sql = $@"
                SELECT 
                    log_id as Id,
                    timestamp as Timestamp,
                    severity as Severity,
                    message as Message,
                    service_name as ServiceName,
                    environment as Environment,
                    attributes_json as AttributesJson
                FROM logs
                WHERE {whereClause}
                ORDER BY timestamp DESC
                LIMIT @PageSize OFFSET @Offset";

            parameters["@PageSize"] = request.PageSize;
            parameters["@Offset"] = offset;

            var rows = await connection.QueryAsync<LogRow>(sql, parameters);

            // Conta total separado
            var countSql = $@"SELECT count() as TotalCount FROM logs WHERE {whereClause}";
            var total = await connection.ExecuteScalarAsync<long>(countSql, parameters);

            var entries = rows.Select(r => new SearchLogs.LogEntry(
                r.Id,
                r.Timestamp,
                r.Severity,
                r.Message,
                r.ServiceName,
                r.Environment,
                ParseAttributes(r.AttributesJson)
            )).ToList();

            return (entries.AsReadOnly(), total);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao pesquisar logs no ClickHouse.");
            return ([], 0);
        }
    }

    /// <inheritdoc />
    public async Task IndexLogAsync(SearchLogs.LogEntry log, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = @"INSERT INTO logs 
                (log_id, timestamp, service_name, environment, severity, message, attributes_json)
                VALUES 
                (@LogId, @Timestamp, @ServiceName, @Environment, @Severity, @Message, @AttributesJson)";

            var attributesJson = System.Text.Json.JsonSerializer.Serialize(log.Attributes);

            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                LogId = log.Id,
                Timestamp = log.Timestamp.UtcDateTime,
                ServiceName = log.ServiceName,
                Environment = log.Environment,
                Severity = log.Severity.ToLowerInvariant(),
                Message = log.Message,
                AttributesJson = attributesJson
            }, cancellationToken: cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao indexar log no ClickHouse.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var result = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickHouse não está saudável ou acessível.");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<TelemetryBackendStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT 
                    count() as TotalDocuments,
                    sum(bytes_on_disk) as TotalSizeBytes,
                    count(DISTINCT toDate(timestamp)) as ActiveDays,
                    max(timestamp) as LastIndexTime
                FROM logs";

            var stats = await connection.QuerySingleOrDefaultAsync<StatsRow>(sql);

            return new TelemetryBackendStats(
                BackendType: "ClickHouse",
                TotalDocuments: stats?.TotalDocuments ?? 0,
                TotalSizeBytes: stats?.TotalSizeBytes ?? 0,
                ActiveIndices: stats?.ActiveDays ?? 0,
                LastIndexTime: stats?.LastIndexTime ?? DateTimeOffset.MinValue
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao obter estatísticas do ClickHouse.");
            return new TelemetryBackendStats("ClickHouse", 0, 0, 0, DateTimeOffset.MinValue);
        }
    }

    private static IReadOnlyDictionary<string, object?> ParseAttributes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, object?>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json) 
                   ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    private ClickHouseConnection CreateConnection()
    {
        return new ClickHouseConnection(_connectionString);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private sealed record LogRow(
        string Id,
        DateTimeOffset Timestamp,
        string Severity,
        string Message,
        string? ServiceName,
        string? Environment,
        string? AttributesJson);

    private sealed record StatsRow(
        long TotalDocuments,
        long TotalSizeBytes,
        int ActiveDays,
        DateTimeOffset LastIndexTime);
}
