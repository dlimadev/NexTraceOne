using Microsoft.Extensions.Configuration;
using Npgsql;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica se o PostgreSQL está acessível e tem versão ≥ 15.
/// Check obrigatório — falha bloqueia o startup.
/// </summary>
public sealed class PostgreSqlPreflightCheck(IConfiguration configuration) : IPreflightCheck
{
    private const string CheckName = "PostgreSQL";

    public async Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
        => [await ExecuteAsync(ct)];

    private async Task<PreflightCheckResult> ExecuteAsync(CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("NexTraceOne")
                               ?? configuration.GetConnectionString("IdentityDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Error,
                "Connection string 'NexTraceOne' is not configured.",
                "Set the ConnectionStrings__NexTraceOne environment variable or configure it in appsettings.json.");
        }

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT version();";
            var result = await cmd.ExecuteScalarAsync(ct) as string ?? string.Empty;

            var majorVersion = ExtractPostgresVersion(result);
            if (majorVersion < 15)
            {
                return new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Error,
                    $"PostgreSQL version {majorVersion} is below the minimum required (15).",
                    "Upgrade PostgreSQL to version 15 or higher.");
            }

            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Ok,
                $"PostgreSQL accessible — {result.Split('\n')[0].Trim()}");
        }
        catch (OperationCanceledException)
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Warning,
                "PostgreSQL check was cancelled.",
                Suggestion: null, IsRequired: false);
        }
        catch (Exception ex)
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Error,
                $"PostgreSQL not accessible: {ex.Message}",
                "Ensure PostgreSQL is running and the connection string is correct.");
        }
    }

    private static int ExtractPostgresVersion(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString)) return 0;
        var parts = versionString.Split(' ');
        for (var i = 0; i < parts.Length; i++)
        {
            if (string.Equals(parts[i], "postgresql", StringComparison.OrdinalIgnoreCase) && i + 1 < parts.Length)
            {
                var versionPart = parts[i + 1].Split('.');
                if (int.TryParse(versionPart[0], out var major))
                    return major;
            }
        }
        return 0;
    }
}
