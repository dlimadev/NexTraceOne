using System.Diagnostics;

using Microsoft.Extensions.Logging;

namespace NexTraceOne.BackgroundWorkers.Backup;

/// <summary>
/// Implementação real que invoca pg_dump como processo externo.
/// Requer pg_dump disponível no PATH ou em /usr/bin/pg_dump.
/// </summary>
internal sealed class PgDumpBackupProcess(ILogger<PgDumpBackupProcess> logger) : IBackupProcess
{
    private static readonly string[] PgDumpPaths =
    [
        "pg_dump",
        "/usr/bin/pg_dump",
        "/usr/local/bin/pg_dump",
    ];

    public async Task DumpAsync(
        string host,
        int port,
        string username,
        string password,
        string database,
        Stream outputStream,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var pgDump = ResolvePgDump();

        var psi = new ProcessStartInfo
        {
            FileName = pgDump,
            Arguments = $"-h {host} -p {port} -U {username} -d {database} -F c",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        psi.Environment["PGPASSWORD"] = password;

        logger.LogInformation("Iniciando pg_dump para base de dados '{Database}' em {Host}:{Port}.", database, host, port);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Não foi possível iniciar pg_dump para '{database}'.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        var copyTask = process.StandardOutput.BaseStream.CopyToAsync(outputStream, cts.Token);
        var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

        await Task.WhenAll(copyTask, process.WaitForExitAsync(cts.Token));

        var stderr = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"pg_dump terminou com código {process.ExitCode} para '{database}': {stderr}");
        }

        if (!string.IsNullOrEmpty(stderr))
            logger.LogWarning("pg_dump para '{Database}' emitiu avisos: {Stderr}", database, stderr);

        logger.LogInformation("pg_dump concluído com sucesso para '{Database}'.", database);
    }

    private static string ResolvePgDump()
    {
        foreach (var path in PgDumpPaths)
        {
            if (File.Exists(path) || (path == "pg_dump"))
                return path;
        }
        return "pg_dump";
    }
}
