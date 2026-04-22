using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de ISbomCoverageReader.
/// Retorna lista vazia — sem dados de SBOM disponíveis.
/// Wave AO.1 — GetSbomCoverageReport.
/// </summary>
public sealed class NullSbomCoverageReader : ISbomCoverageReader
{
    public Task<IReadOnlyList<ISbomCoverageReader.ServiceSbomEntry>> ListByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ISbomCoverageReader.ServiceSbomEntry>>([]);
}
