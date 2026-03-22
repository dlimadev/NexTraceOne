namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Surface de acesso a dados de runtime para o subdomínio Reliability.
/// Abstrai o acesso a RuntimeIntelligenceDbContext dentro do mesmo módulo OI.
/// </summary>
public interface IReliabilityRuntimeSurface
{
    Task<RuntimeServiceSignal?> GetLatestSignalAsync(string serviceName, string environment, CancellationToken ct);
    Task<IReadOnlyList<RuntimeServiceSignal>> GetLatestSignalsAllServicesAsync(string? environment, CancellationToken ct);
    Task<decimal?> GetObservabilityScoreAsync(string serviceName, string environment, CancellationToken ct);
    Task<IReadOnlyDictionary<string, decimal>> GetObservabilityScoresAllServicesAsync(string? environment, CancellationToken ct);
}

/// <summary>Sinal de runtime de um serviço extraído do RuntimeSnapshot mais recente.</summary>
public sealed record RuntimeServiceSignal(
    string ServiceName,
    string Environment,
    string HealthStatus,
    decimal ErrorRate,
    decimal P99LatencyMs,
    decimal RequestsPerSecond,
    DateTimeOffset CapturedAt);
