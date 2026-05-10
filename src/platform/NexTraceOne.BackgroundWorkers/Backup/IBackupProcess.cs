namespace NexTraceOne.BackgroundWorkers.Backup;

/// <summary>
/// Abstracção sobre pg_dump para permitir testes unitários sem processo externo.
/// </summary>
public interface IBackupProcess
{
    /// <summary>
    /// Executa pg_dump e escreve o output para o stream fornecido.
    /// </summary>
    Task DumpAsync(
        string host,
        int port,
        string username,
        string password,
        string database,
        Stream outputStream,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
