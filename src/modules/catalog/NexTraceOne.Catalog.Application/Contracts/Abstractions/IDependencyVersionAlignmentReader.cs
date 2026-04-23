namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>Wave AR.3 — GetDependencyVersionAlignmentReport. Uses SbomRecord data.</summary>
public interface IDependencyVersionAlignmentReader
{
    Task<IReadOnlyList<ComponentVersionEntry>> ListComponentVersionsByTenantAsync(
        string tenantId, CancellationToken ct);

    public sealed record ComponentVersionEntry(
        string ServiceId,
        string ServiceName,
        string TeamId,
        string ServiceTier,
        string ComponentName,
        string ComponentVersion,
        bool HasKnownCve,
        DateTimeOffset IngestedAt);
}
