namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Surface de acesso a dados de incidentes para o subdomínio Reliability.
/// Abstrai o acesso a IncidentDbContext dentro do mesmo módulo OI.
/// </summary>
public interface IReliabilityIncidentSurface
{
    Task<IReadOnlyList<ReliabilityIncidentSignal>> GetActiveIncidentsAsync(string serviceName, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<ReliabilityIncidentSignal>> GetAllServicesIncidentSignalsAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<ReliabilityIncidentSignal>> GetTeamIncidentsAsync(string teamId, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<ReliabilityIncidentSignal>> GetDomainIncidentsAsync(string domainId, Guid tenantId, CancellationToken ct);
    Task<bool> HasRunbookAsync(string serviceId, CancellationToken ct);
}

/// <summary>Sinal de incidente extraído do IncidentRecord para cálculo de score de confiabilidade.</summary>
public sealed record ReliabilityIncidentSignal(
    string ServiceId,
    string ServiceName,
    string TeamName,
    string Severity,
    string Status,
    DateTimeOffset DetectedAt);
