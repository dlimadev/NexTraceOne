namespace NexTraceOne.BackgroundWorkers.Health;

/// <summary>
/// Lê métricas de saúde da plataforma para o PlatformHealthMonitorJob.
/// Abstracção necessária para testabilidade do job de monitorização.
/// </summary>
public interface IPlatformHealthReader
{
    /// <summary>Conta mensagens de outbox pendentes de processamento.</summary>
    Task<long> CountPendingOutboxAsync(CancellationToken cancellationToken);

    /// <summary>Retorna informações de uso do disco principal.</summary>
    DiskUsageInfo GetPrimaryDiskUsage();
}

/// <summary>
/// Informações de uso de disco retornadas pelo IPlatformHealthReader.
/// </summary>
public sealed record DiskUsageInfo(long TotalBytes, long UsedBytes)
{
    /// <summary>Percentagem de disco utilizado (0–100).</summary>
    public double UsedPercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100.0 : 0;

    /// <summary>Valor sentinela quando a informação de disco não está disponível.</summary>
    public static DiskUsageInfo Unknown => new(0, 0);
}
