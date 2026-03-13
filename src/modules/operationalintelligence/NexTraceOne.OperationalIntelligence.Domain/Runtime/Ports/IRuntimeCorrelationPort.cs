namespace NexTraceOne.RuntimeIntelligence.Domain.Ports;

/// <summary>
/// Porta de correlação de sinais de runtime com mudanças registradas.
/// Permite identificar anomalias e impactos operacionais vinculados a releases específicas.
/// Preparada para futura extração como Runtime Stream Processor.
/// </summary>
public interface IRuntimeCorrelationPort
{
    /// <summary>
    /// Correlaciona sinais de runtime com uma release específica.
    /// </summary>
    Task<bool> CorrelateWithReleaseAsync(Guid releaseId, string signalType, DateTimeOffset timestamp, CancellationToken cancellationToken = default);
}
