using Npgsql;

namespace NexTraceOne.ApiHost.OnPrem;

/// <summary>
/// Serviço de diagnóstico de saúde do PostgreSQL.
/// Executa queries directas em pg_stat_* para retornar métricas de saúde
/// sem depender de EF Core, garantindo leitura sempre actual.
/// Disponível apenas para PlatformAdmin via GET /api/v1/platform/database-health.
/// </summary>
public sealed class DatabaseHealthService(IConfiguration configuration)
{
    public async Task<DatabaseHealthReport> GetHealthAsync(CancellationToken ct = default)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:PostgreSQL"]
            ?? configuration["ConnectionStrings:Governance"];

        if (string.IsNullOrWhiteSpace(connStr))
        {
            return new DatabaseHealthReport(
                Available: false,
                Error: "No PostgreSQL connection string configured.",
                Version: null,
                UptimeMinutes: 0,
                ActiveConnections: 0,
                MaxConnections: 0,
                TotalSizeGb: 0,
                Schemas: [],
                BloatSignals: [],
                SlowQueryCount: 0,
                SlowQueries: [],
                CheckedAt: DateTimeOffset.UtcNow);
        }

        try
        {
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            var version      = await GetVersionAsync(conn, ct);
            var conns        = await GetConnectionsAsync(conn, ct);
            var schemas      = await GetSchemaSizesAsync(conn, ct);
            var bloat        = await GetBloatSignalsAsync(conn, ct);
            var slowQueries  = await GetSlowQueriesAsync(conn, ct);
            var uptimeMin    = await GetUptimeMinutesAsync(conn, ct);
            var totalSizeGb  = schemas.Sum(s => s.SizeGb);

            return new DatabaseHealthReport(
                Available:         true,
                Error:             null,
                Version:           version,
                UptimeMinutes:     uptimeMin,
                ActiveConnections: conns.Active,
                MaxConnections:    conns.Max,
                TotalSizeGb:       totalSizeGb,
                Schemas:           schemas,
                BloatSignals:      bloat,
                SlowQueryCount:    slowQueries.Count,
                SlowQueries:       slowQueries,
                CheckedAt:         DateTimeOffset.UtcNow);
        }
        catch (NpgsqlException ex)
        {
            return new DatabaseHealthReport(
                Available: false,
                Error: $"PostgreSQL query failed: {ex.Message}",
                Version: null,
                UptimeMinutes: 0,
                ActiveConnections: 0,
                MaxConnections: 0,
                TotalSizeGb: 0,
                Schemas: [],
                BloatSignals: [],
                SlowQueryCount: 0,
                SlowQueries: [],
                CheckedAt: DateTimeOffset.UtcNow);
        }
    }

    private static async Task<string> GetVersionAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("SELECT version()", conn);
        var result = await cmd.ExecuteScalarAsync(ct);
        var full   = result?.ToString() ?? "Unknown";
        var parts  = full.Split(' ');
        return parts.Length >= 2 ? $"{parts[0]} {parts[1]}" : full;
    }

    private static async Task<(int Active, int Max)> GetConnectionsAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            SELECT
                (SELECT count(*) FROM pg_stat_activity WHERE state = 'active')::int AS active,
                current_setting('max_connections')::int AS max
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (await rdr.ReadAsync(ct))
        {
            return (rdr.GetInt32(0), rdr.GetInt32(1));
        }

        return (0, 100);
    }

    private static async Task<IReadOnlyList<DbSchemaSize>> GetSchemaSizesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            SELECT
                schemaname,
                round(sum(pg_total_relation_size(schemaname||'.'||tablename))::numeric / 1073741824, 3)::float8 AS size_gb,
                count(*)::int AS table_count
            FROM pg_tables
            WHERE schemaname NOT IN ('pg_catalog','information_schema','pg_toast')
            GROUP BY schemaname
            ORDER BY size_gb DESC
            LIMIT 20
            """;
        await using var cmd    = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<DbSchemaSize>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new DbSchemaSize(
                Schema:     reader.GetString(0),
                SizeGb:     reader.IsDBNull(1) ? 0.0 : reader.GetDouble(1),
                TableCount: reader.GetInt32(2)));
        }

        return result;
    }

    private static async Task<IReadOnlyList<DbBloatSignal>> GetBloatSignalsAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            SELECT
                schemaname,
                tablename,
                round((n_dead_tup * 100.0 / NULLIF(n_live_tup + n_dead_tup, 0))::numeric, 1)::float8 AS bloat_pct
            FROM pg_stat_user_tables
            WHERE n_live_tup > 1000
              AND (n_dead_tup * 100.0 / NULLIF(n_live_tup + n_dead_tup, 0)) > 10
            ORDER BY bloat_pct DESC
            LIMIT 10
            """;
        await using var cmd    = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<DbBloatSignal>();
        while (await reader.ReadAsync(ct))
        {
            var pct = reader.IsDBNull(2) ? 0.0 : reader.GetDouble(2);
            result.Add(new DbBloatSignal(
                Schema:   reader.GetString(0),
                Table:    reader.GetString(1),
                BloatPct: pct,
                Severity: pct >= 30 ? "High" : pct >= 20 ? "Medium" : "Low"));
        }

        return result;
    }

    private static async Task<IReadOnlyList<DbSlowQuery>> GetSlowQueriesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        // Graceful fallback if pg_stat_statements is not installed
        const string checkSql = "SELECT count(*) FROM pg_extension WHERE extname = 'pg_stat_statements'";
        await using var checkCmd = new NpgsqlCommand(checkSql, conn);
        var extCount             = (long?)await checkCmd.ExecuteScalarAsync(ct) ?? 0L;
        if (extCount == 0)
        {
            return [];
        }

        const string sql = """
            SELECT
                left(query, 200) AS query_preview,
                round(mean_exec_time::numeric, 0)::int AS mean_ms,
                calls::bigint
            FROM pg_stat_statements
            WHERE mean_exec_time > 1000
            ORDER BY mean_exec_time DESC
            LIMIT 5
            """;
        await using var cmd    = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<DbSlowQuery>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new DbSlowQuery(
                QueryPreview: reader.GetString(0),
                MeanMs:       reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                Calls:        reader.GetInt64(2)));
        }

        return result;
    }

    private static async Task<long> GetUptimeMinutesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = "SELECT EXTRACT(EPOCH FROM (now() - pg_postmaster_start_time()))::bigint / 60";
        await using var cmd = new NpgsqlCommand(sql, conn);
        var result          = await cmd.ExecuteScalarAsync(ct);
        return result is long l ? l : Convert.ToInt64(result ?? 0);
    }
}

/// <summary>Relatório completo de saúde do PostgreSQL.</summary>
public sealed record DatabaseHealthReport(
    bool Available,
    string? Error,
    string? Version,
    long UptimeMinutes,
    int ActiveConnections,
    int MaxConnections,
    double TotalSizeGb,
    IReadOnlyList<DbSchemaSize> Schemas,
    IReadOnlyList<DbBloatSignal> BloatSignals,
    int SlowQueryCount,
    IReadOnlyList<DbSlowQuery> SlowQueries,
    DateTimeOffset CheckedAt);

/// <summary>Tamanho de um schema PostgreSQL.</summary>
public sealed record DbSchemaSize(string Schema, double SizeGb, int TableCount);

/// <summary>Sinal de bloat numa tabela PostgreSQL.</summary>
public sealed record DbBloatSignal(string Schema, string Table, double BloatPct, string Severity);

/// <summary>Query lenta detectada via pg_stat_statements.</summary>
public sealed record DbSlowQuery(string QueryPreview, int MeanMs, long Calls);
