using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.BackgroundWorkers.Backup;
using NexTraceOne.BackgroundWorkers.Configuration;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// W3-03: Coordena backups automatizados de todas as bases de dados PostgreSQL.
///
/// Comportamento:
/// - Executa diariamente (cada 24h), com delay inicial de 60s.
/// - Cria pg_dump para cada base de dados definida em BackupOptions.Databases.
/// - Comprime com GZip e gera manifesto SHA-256.
/// - Aplica política de retenção: remove backups mais antigos que RetentionDays dias.
/// - Falha numa base de dados não interrompe o backup das restantes.
/// </summary>
public sealed class BackupCoordinatorJob(
    IBackupProcess backupProcess,
    WorkerJobHealthRegistry jobHealthRegistry,
    IOptions<BackupOptions> options,
    ILogger<BackupCoordinatorJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "backup-coordinator-job";

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("BackupCoordinatorJob iniciado — intervalo {Interval}.", Interval);

        try
        {
            await Task.Delay(StartDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                jobHealthRegistry.MarkStarted(HealthCheckName);
                await RunBackupCycleAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, "Ciclo de backup falhou.");
                logger.LogError(ex, "Erro no ciclo do BackupCoordinatorJob.");
            }
        }

        logger.LogInformation("BackupCoordinatorJob parado.");
    }

    internal async Task RunBackupCycleAsync(CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var timestamp = DateTimeOffset.UtcNow;
        var sessionDir = Path.Combine(opts.OutputDirectory, timestamp.ToString("yyyy-MM-dd_HH-mm-ss"));

        Directory.CreateDirectory(sessionDir);

        var (host, port, username, password) = ParsePrimaryConnectionString(opts);
        var manifest = new BackupManifest(timestamp, opts.Databases.Count);
        var succeeded = 0;

        foreach (var database in opts.Databases)
        {
            try
            {
                var entry = await BackupDatabaseAsync(
                    host, port, username, password, database,
                    sessionDir, opts.DumpTimeoutMinutes, cancellationToken);

                manifest.Entries.Add(entry);
                succeeded++;
                logger.LogInformation("Backup concluído para '{Database}': {File}.", database, entry.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha no backup de '{Database}' — a continuar com as restantes.", database);
                manifest.Entries.Add(new BackupManifestEntry(database, string.Empty, 0, string.Empty, false, ex.Message));
            }
        }

        await WriteManifestAsync(sessionDir, manifest, cancellationToken);
        ApplyRetentionPolicy(opts);

        logger.LogInformation(
            "Ciclo de backup concluído: {Succeeded}/{Total} bases de dados com sucesso.",
            succeeded, opts.Databases.Count);
    }

    private async Task<BackupManifestEntry> BackupDatabaseAsync(
        string host, int port, string username, string password,
        string database, string sessionDir, int timeoutMinutes,
        CancellationToken cancellationToken)
    {
        var fileName = $"{database}_{DateTimeOffset.UtcNow:HHmmss}.dump.gz";
        var filePath = Path.Combine(sessionDir, fileName);

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);

        await backupProcess.DumpAsync(
            host, port, username, password, database,
            gzipStream, TimeSpan.FromMinutes(timeoutMinutes), cancellationToken);

        await gzipStream.FlushAsync(cancellationToken);
        await fileStream.FlushAsync(cancellationToken);

        var fileInfo = new FileInfo(filePath);
        var sha256 = await ComputeSha256Async(filePath, cancellationToken);

        return new BackupManifestEntry(database, fileName, fileInfo.Length, sha256, true, null);
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task WriteManifestAsync(
        string sessionDir, BackupManifest manifest, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(sessionDir, "manifest.json");
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(manifestPath, json, Encoding.UTF8, cancellationToken);
    }

    private void ApplyRetentionPolicy(BackupOptions opts)
    {
        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-opts.RetentionDays);
            var parentDir = new DirectoryInfo(opts.OutputDirectory);

            if (!parentDir.Exists)
                return;

            foreach (var dir in parentDir.GetDirectories())
            {
                if (dir.CreationTimeUtc < cutoff)
                {
                    dir.Delete(recursive: true);
                    logger.LogInformation("Backup expirado removido: {Dir}.", dir.Name);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erro ao aplicar política de retenção de backups.");
        }
    }

    private static (string host, int port, string username, string password) ParsePrimaryConnectionString(BackupOptions opts)
    {
        // Fallback: tenta lê la de ambiente ou usa defaults de desenvolvimento
        var cs = Environment.GetEnvironmentVariable("NEXTRACEONE_CONNECTION_STRING")
                 ?? "Host=localhost;Port=5432;Username=nextraceone;Password=dev";

        var parts = cs.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        var host = parts.GetValueOrDefault("Host", "localhost");
        var portStr = parts.GetValueOrDefault("Port", "5432");
        var port = int.TryParse(portStr, out var p) ? p : 5432;
        var username = parts.GetValueOrDefault("Username", parts.GetValueOrDefault("User ID", "nextraceone"));
        var password = parts.GetValueOrDefault("Password", string.Empty);

        return (host, port, username, password);
    }
}

/// <summary>Manifesto de backup gerado após cada ciclo.</summary>
internal sealed class BackupManifest(DateTimeOffset timestamp, int expectedDatabases)
{
    public DateTimeOffset Timestamp { get; } = timestamp;
    public int ExpectedDatabases { get; } = expectedDatabases;
    public List<BackupManifestEntry> Entries { get; } = [];
}

/// <summary>Entrada do manifesto para uma base de dados.</summary>
internal sealed record BackupManifestEntry(
    string Database,
    string FileName,
    long FileSizeBytes,
    string Sha256,
    bool Success,
    string? ErrorMessage);
