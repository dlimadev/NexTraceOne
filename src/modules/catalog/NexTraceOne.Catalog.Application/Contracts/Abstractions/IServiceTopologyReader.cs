namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>Wave AR.1 — GetServiceTopologyHealthReport.</summary>
public interface IServiceTopologyReader
{
    Task<IReadOnlyList<ServiceDependencyEntry>> ListDependenciesByTenantAsync(
        string tenantId, int freshnessThresholdDays, CancellationToken ct);

    Task<IReadOnlyList<ServiceNodeEntry>> ListServiceNodesByTenantAsync(
        string tenantId, CancellationToken ct);

    public sealed record ServiceDependencyEntry(
        string SourceServiceId,
        string TargetServiceId,
        string SourceServiceTier,
        string TargetServiceTier,
        DateTimeOffset LastUpdatedAt);

    public sealed record ServiceNodeEntry(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        bool IsCustomerFacing,
        DateTimeOffset LastUpdatedAt);
}
