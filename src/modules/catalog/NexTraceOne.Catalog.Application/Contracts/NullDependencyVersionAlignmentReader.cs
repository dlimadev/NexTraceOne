using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>Wave AR.3 — honest-null IDependencyVersionAlignmentReader.</summary>
public sealed class NullDependencyVersionAlignmentReader : IDependencyVersionAlignmentReader
{
    public Task<IReadOnlyList<IDependencyVersionAlignmentReader.ComponentVersionEntry>> ListComponentVersionsByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IDependencyVersionAlignmentReader.ComponentVersionEntry>>([]);
}
