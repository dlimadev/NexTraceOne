namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de endpoints de alto tráfego.
/// Por omissão satisfeita por <c>NullHighTrafficEndpointReader</c> (honest-null).
/// Wave AZ.2 — GetHighTrafficEndpointRiskReport.
/// </summary>
public interface IHighTrafficEndpointReader
{
    Task<IReadOnlyList<EndpointTrafficEntry>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Dados de tráfego de um endpoint no período.</summary>
    public sealed record EndpointTrafficEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        string EndpointPath,
        string HttpMethod,
        long CallVolume,
        double RpsAvg,
        double LatencyP50Ms,
        double LatencyP95Ms,
        double LatencyP99Ms,
        double ErrorRatePct,
        bool IsDocumentedInContract,
        bool IsDeprecatedInContract,
        bool HasActiveSlo,
        bool WasChaosTestedInPeriod,
        bool ServiceHasCriticalTier);
}
