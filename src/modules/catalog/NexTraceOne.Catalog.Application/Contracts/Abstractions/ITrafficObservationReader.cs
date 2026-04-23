namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de tráfego real observado por serviço.
/// Por omissão satisfeita por <c>NullTrafficObservationReader</c> (honest-null).
/// Wave AZ.1 — GetRuntimeTrafficContractDeviationReport.
/// </summary>
public interface ITrafficObservationReader
{
    Task<IReadOnlyList<ServiceTrafficObservationEntry>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<IReadOnlyList<DailyDeviationSnapshot>> GetDeviationTrendAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Entrada de tráfego observado por serviço no período.</summary>
    public sealed record ServiceTrafficObservationEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        IReadOnlyList<ObservedEndpoint> ObservedEndpoints,
        IReadOnlyList<string> ObservedConsumerIds,
        IReadOnlyList<string> RegisteredConsumerIds,
        IReadOnlyList<string> ContractedEndpoints,
        IReadOnlyList<string> ContractedStatusCodes,
        IReadOnlyList<StatusCodeObservation> ObservedStatusCodes,
        int TotalPayloadValidationEvents,
        int PayloadDeviationEvents);

    /// <summary>Endpoint observado com métricas.</summary>
    public sealed record ObservedEndpoint(
        string Method,
        string Path,
        long CallCount);

    /// <summary>Status code observado com contagem.</summary>
    public sealed record StatusCodeObservation(
        int StatusCode,
        long Count);

    /// <summary>Snapshot diário de desvios para tendência.</summary>
    public sealed record DailyDeviationSnapshot(
        int DayOffset,
        int DeviationCount);
}
