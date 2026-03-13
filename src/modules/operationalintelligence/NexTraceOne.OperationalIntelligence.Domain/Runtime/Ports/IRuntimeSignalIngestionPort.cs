namespace NexTraceOne.RuntimeIntelligence.Domain.Ports;

/// <summary>
/// Porta de ingestão de sinais de runtime.
/// Define o contrato para recebimento de dados operacionais de serviços monitorados.
/// Preparada para futura extração como Runtime Agent independente.
/// </summary>
public interface IRuntimeSignalIngestionPort
{
    /// <summary>
    /// Ingere um sinal de runtime recebido de uma fonte externa.
    /// </summary>
    Task IngestSignalAsync(string sourceSystem, string signalType, string payload, CancellationToken cancellationToken = default);
}
