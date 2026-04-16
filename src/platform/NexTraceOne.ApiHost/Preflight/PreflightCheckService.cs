using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace NexTraceOne.ApiHost.Preflight;

/// <summary>
/// Serviço responsável por executar todos os preflight checks antes do primeiro login.
/// Endpoint público GET /preflight — acessível sem autenticação para diagnóstico pré-arranque.
///
/// Checks obrigatórios (Error se falhar):
///   - PostgreSQL acessível e versão ≥ 15
///   - JWT Secret configurado e ≥ 32 caracteres
///   - Pelo menos uma connection string configurada
///
/// Checks de aviso (Warning se não disponível):
///   - Disco disponível ≥ 5 GB
///   - RAM disponível ≥ 4 GB
///   - Portas 8080/8090 livres
///   - Ollama acessível em :11434
///   - SMTP configurado
///   - OTel Collector acessível
///   - CORS Origins configuradas
/// </summary>
public sealed class PreflightCheckService(IConfiguration configuration)
{
    private const long MinDiskGb = 5;
    private const long MinRamGb = 4;
    private const int MinJwtSecretLength = 32;

    /// <summary>
    /// Executa todos os checks e retorna o relatório consolidado.
    /// Nunca lança excepção — erros internos são capturados e reportados como Warning.
    /// </summary>
    public async Task<PreflightReport> RunAsync(CancellationToken ct = default)
    {
        var checks = new List<PreflightCheckResult>();

        checks.Add(await CheckPostgreSqlAsync(ct));
        checks.Add(CheckJwtSecret());
        checks.Add(CheckConnectionStrings());
        checks.Add(CheckDiskSpace());
        checks.Add(CheckRamAvailable());
        checks.AddRange(CheckPorts());
        checks.Add(await CheckOllamaAsync(ct));
        checks.Add(CheckSmtp());
        checks.Add(await CheckOtelCollectorAsync(ct));
        checks.Add(CheckCorsOrigins());

        var hasErrors = checks.Any(c => c.Status == PreflightCheckStatus.Error && c.IsRequired);
        var hasWarnings = checks.Any(c => c.Status == PreflightCheckStatus.Warning);

        var overall = hasErrors
            ? PreflightCheckStatus.Error
            : hasWarnings ? PreflightCheckStatus.Warning : PreflightCheckStatus.Ok;

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

        return new PreflightReport(
            OverallStatus: overall,
            Checks: checks,
            IsReadyToStart: !hasErrors,
            CheckedAt: DateTimeOffset.UtcNow,
            Version: version);
    }

    // ─── PostgreSQL ───────────────────────────────────────────────────────────

    private async Task<PreflightCheckResult> CheckPostgreSqlAsync(CancellationToken ct)
    {
        const string name = "PostgreSQL";
        var connectionString = configuration.GetConnectionString("NexTraceOne")
                               ?? configuration.GetConnectionString("IdentityDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Error,
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

            // Extract major version from "PostgreSQL 16.2 on ..."
            var majorVersion = ExtractPostgresVersion(result);
            if (majorVersion < 15)
            {
                return new PreflightCheckResult(
                    name, PreflightCheckStatus.Error,
                    $"PostgreSQL version {majorVersion} is below the minimum required (15).",
                    "Upgrade PostgreSQL to version 15 or higher.");
            }

            return new PreflightCheckResult(
                name, PreflightCheckStatus.Ok,
                $"PostgreSQL accessible — {result.Split('\n')[0].Trim()}");
        }
        catch (OperationCanceledException)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                "PostgreSQL check was cancelled.",
                Suggestion: null, IsRequired: false);
        }
        catch (Exception ex)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Error,
                $"PostgreSQL not accessible: {ex.Message}",
                "Ensure PostgreSQL is running and the connection string is correct.");
        }
    }

    private static int ExtractPostgresVersion(string versionString)
    {
        // "PostgreSQL 16.2 on ..." → 16
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

    // ─── JWT Secret ───────────────────────────────────────────────────────────

    private PreflightCheckResult CheckJwtSecret()
    {
        const string name = "JWT Secret";
        var secret = configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Error,
                "Jwt:Secret is not configured.",
                "Set the Jwt__Secret environment variable (minimum 32 characters). Generate with: openssl rand -base64 48");
        }

        if (secret.Length < MinJwtSecretLength)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Error,
                $"Jwt:Secret is {secret.Length} characters — below the minimum of {MinJwtSecretLength}.",
                "Generate a stronger JWT secret with: openssl rand -base64 48");
        }

        return new PreflightCheckResult(
            name, PreflightCheckStatus.Ok,
            $"JWT Secret configured — {secret.Length} characters.");
    }

    // ─── Connection Strings ───────────────────────────────────────────────────

    private PreflightCheckResult CheckConnectionStrings()
    {
        const string name = "Connection Strings";
        var connStrings = configuration.GetSection("ConnectionStrings");

        if (!connStrings.Exists())
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Error,
                "ConnectionStrings section is missing from configuration.",
                "Add the ConnectionStrings section with at least the NexTraceOne connection string.");
        }

        var configured = connStrings.GetChildren()
            .Count(c => !string.IsNullOrWhiteSpace(c.Value));

        var total = connStrings.GetChildren().Count();

        if (configured == 0)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Error,
                "No connection strings are configured.",
                "Configure at least ConnectionStrings__NexTraceOne.");
        }

        return new PreflightCheckResult(
            name, PreflightCheckStatus.Ok,
            $"Connection strings: {configured}/{total} configured.");
    }

    // ─── Disk Space ───────────────────────────────────────────────────────────

    private static PreflightCheckResult CheckDiskSpace()
    {
        const string name = "Disk Space";
        try
        {
            var drive = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .OrderByDescending(d => d.AvailableFreeSpace)
                .FirstOrDefault();

            if (drive is null)
            {
                return new PreflightCheckResult(
                    name, PreflightCheckStatus.Warning,
                    "No fixed drives detected.",
                    IsRequired: false, Suggestion: null);
            }

            var availableGb = drive.AvailableFreeSpace / (1024L * 1024L * 1024L);
            var totalGb = drive.TotalSize / (1024L * 1024L * 1024L);

            if (availableGb < MinDiskGb)
            {
                return new PreflightCheckResult(
                    name, PreflightCheckStatus.Warning,
                    $"Low disk space: {availableGb} GB available on {drive.Name} (minimum recommended: {MinDiskGb} GB).",
                    "Free up disk space or mount a larger volume.",
                    IsRequired: false);
            }

            return new PreflightCheckResult(
                name, PreflightCheckStatus.Ok,
                $"Disk: {availableGb} GB available / {totalGb} GB total on {drive.Name}.");
        }
        catch (Exception ex)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                $"Could not check disk space: {ex.Message}",
                IsRequired: false, Suggestion: null);
        }
    }

    // ─── RAM ──────────────────────────────────────────────────────────────────

    private static PreflightCheckResult CheckRamAvailable()
    {
        const string name = "RAM";
        try
        {
            // GC.GetGCMemoryInfo provides total physical memory on .NET 5+
            var memInfo = GC.GetGCMemoryInfo();
            var totalRamGb = memInfo.TotalAvailableMemoryBytes / (1024L * 1024L * 1024L);

            if (totalRamGb < MinRamGb)
            {
                return new PreflightCheckResult(
                    name, PreflightCheckStatus.Warning,
                    $"Available RAM: {totalRamGb} GB (minimum recommended: {MinRamGb} GB).",
                    "Increase server RAM to at least 4 GB for stable operation.",
                    IsRequired: false);
            }

            var processRamMb = Process.GetCurrentProcess().WorkingSet64 / (1024L * 1024L);
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Ok,
                $"RAM: {totalRamGb} GB total | Process RSS: {processRamMb} MB.");
        }
        catch (Exception ex)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                $"Could not check RAM: {ex.Message}",
                IsRequired: false, Suggestion: null);
        }
    }

    // ─── Ports ────────────────────────────────────────────────────────────────

    private static IEnumerable<PreflightCheckResult> CheckPorts()
    {
        int[] portsToCheck = [8080, 8090];

        foreach (var port in portsToCheck)
        {
            var inUse = IsPortInUse(port);
            yield return inUse
                ? new PreflightCheckResult(
                    $"Port {port}", PreflightCheckStatus.Warning,
                    $"Port {port} appears to be in use by another process.",
                    $"Ensure port {port} is available or reconfigure NexTraceOne to use a different port.",
                    IsRequired: false)
                : new PreflightCheckResult(
                    $"Port {port}", PreflightCheckStatus.Ok,
                    $"Port {port} is available.");
        }
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            using var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch
        {
            return true;
        }
    }

    // ─── Ollama ───────────────────────────────────────────────────────────────

    private async Task<PreflightCheckResult> CheckOllamaAsync(CancellationToken ct)
    {
        const string name = "Ollama (AI Local)";
        var ollamaBaseUrl = configuration["AiRuntime:Ollama:BaseUrl"] ?? "http://localhost:11434";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await http.GetAsync($"{ollamaBaseUrl}/api/tags", ct);
            return response.IsSuccessStatusCode
                ? new PreflightCheckResult(
                    name, PreflightCheckStatus.Ok,
                    $"Ollama accessible at {ollamaBaseUrl}.")
                : new PreflightCheckResult(
                    name, PreflightCheckStatus.Warning,
                    $"Ollama at {ollamaBaseUrl} returned HTTP {(int)response.StatusCode}.",
                    "Start Ollama and ensure the configured model is pulled. AI features will be unavailable.",
                    IsRequired: false);
        }
        catch (OperationCanceledException)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                "Ollama check timed out or was cancelled. AI features may be unavailable.",
                $"Ensure Ollama is running and accessible at {ollamaBaseUrl}.",
                IsRequired: false);
        }
        catch
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                $"Ollama not detected at {ollamaBaseUrl}. AI features will be unavailable.",
                $"Install and start Ollama (https://ollama.ai) to enable local AI. Not required for core platform functions.",
                IsRequired: false);
        }
    }

    // ─── SMTP ─────────────────────────────────────────────────────────────────

    private PreflightCheckResult CheckSmtp()
    {
        const string name = "SMTP (Email)";
        var smtpHost = configuration["Smtp:Host"];

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                "SMTP not configured — email notifications are disabled.",
                "Set Smtp__Host, Smtp__Port, Smtp__Username, Smtp__Password and Smtp__From to enable notifications.",
                IsRequired: false);
        }

        return new PreflightCheckResult(
            name, PreflightCheckStatus.Ok,
            $"SMTP configured — host: {smtpHost}.");
    }

    // ─── OTel Collector ───────────────────────────────────────────────────────

    private async Task<PreflightCheckResult> CheckOtelCollectorAsync(CancellationToken ct)
    {
        const string name = "OTel Collector";
        var otelEndpoint = configuration["OpenTelemetry:Endpoint"]
                           ?? configuration["Telemetry:Endpoint"]
                           ?? "http://localhost:4317";

        // Skip if OTel is disabled in config
        var otelEnabled = configuration.GetValue<bool?>("OpenTelemetry:Enabled") ?? true;
        if (!otelEnabled)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Ok,
                "OTel Collector disabled by configuration — skipped.",
                IsRequired: false, Suggestion: null);
        }

        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync("localhost", 4317, ct).AsTask();
            if (await Task.WhenAny(connectTask, Task.Delay(2000, ct)) == connectTask && !connectTask.IsFaulted)
            {
                return new PreflightCheckResult(
                    name, PreflightCheckStatus.Ok,
                    "OTel Collector accessible at :4317.");
            }

            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                "OTel Collector not accessible at :4317 — distributed tracing disabled.",
                "Start an OpenTelemetry Collector or disable OTel in configuration.",
                IsRequired: false);
        }
        catch
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                "OTel Collector not accessible — distributed tracing disabled.",
                "Start an OpenTelemetry Collector or set OpenTelemetry__Enabled=false to suppress this warning.",
                IsRequired: false);
        }
    }

    // ─── CORS ─────────────────────────────────────────────────────────────────

    private PreflightCheckResult CheckCorsOrigins()
    {
        const string name = "CORS Origins";
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (origins.Length == 0)
        {
            return new PreflightCheckResult(
                name, PreflightCheckStatus.Warning,
                "No CORS origins configured — browser clients from external domains will be blocked.",
                "Set Cors__AllowedOrigins with the URL(s) of the NexTraceOne frontend (e.g., https://nextraceone.acme.com).",
                IsRequired: false);
        }

        return new PreflightCheckResult(
            name, PreflightCheckStatus.Ok,
            $"CORS configured — {origins.Length} origin(s): {string.Join(", ", origins)}.");
    }
}
