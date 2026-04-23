using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IDependencyProvenanceReader.
/// Retorna lista vazia — sem dados de SBOM para análise de proveniência.
/// Wave AO.2 — GetDependencyProvenanceReport.
/// </summary>
public sealed class NullDependencyProvenanceReader : IDependencyProvenanceReader
{
    public Task<IReadOnlyList<IDependencyProvenanceReader.ComponentProvenanceEntry>> ListComponentsByTenantAsync(
        string tenantId,
        IReadOnlyList<string> approvedRegistries,
        IReadOnlyList<string> highRiskLicenses,
        int spofThreshold,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IDependencyProvenanceReader.ComponentProvenanceEntry>>([]);
}
