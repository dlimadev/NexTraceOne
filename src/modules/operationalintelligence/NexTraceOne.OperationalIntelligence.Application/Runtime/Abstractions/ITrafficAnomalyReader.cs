namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de anomalias de tráfego por serviço.
/// Por omissão satisfeita por <c>NullTrafficAnomalyReader</c> (honest-null).
/// Wave AZ.3 — GetTrafficAnomalyReport.
/// </summary>
public interface ITrafficAnomalyReader
{
    Task<IReadOnlyList<ServiceTrafficAnomalyEntry>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<IReadOnlyList<TimelineEvent>> GetTimelineEventsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Dados de anomalias de tráfego por serviço.</summary>
    public sealed record ServiceTrafficAnomalyEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        IReadOnlyList<AnomalyObservation> Anomalies,
        double BaselineRps,
        double BaselineErrorRatePct,
        double BaselineLatencyP95Ms);

    /// <summary>Observação de anomalia detectada.</summary>
    public sealed record AnomalyObservation(
        string AnomalyType,
        DateTimeOffset DetectedAt,
        DateTimeOffset? ResolvedAt,
        double ObservedValue,
        double BaselineValue,
        string AnomalyCorrelation,
        string? CorrelatedEventId);

    /// <summary>Evento da linha de tempo (release ou incidente).</summary>
    public sealed record TimelineEvent(
        DateTimeOffset OccurredAt,
        string EventType,
        string EventId,
        string Description);
}
