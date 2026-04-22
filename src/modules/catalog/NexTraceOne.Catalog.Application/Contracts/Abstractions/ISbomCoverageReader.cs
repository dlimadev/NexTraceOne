namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de cobertura de SBOM por serviço.
/// Agrega dados de SbomRecord para análise de freshness e vulnerabilidades.
/// Por omissão satisfeita por <c>NullSbomCoverageReader</c> (honest-null).
/// Wave AO.1 — Supply Chain &amp; Dependency Provenance.
/// </summary>
public interface ISbomCoverageReader
{
    /// <summary>Lista todos os serviços do tenant com dados de SBOM (ou ausência).</summary>
    Task<IReadOnlyList<ServiceSbomEntry>> ListByTenantAsync(
        string tenantId,
        CancellationToken ct);

    /// <summary>Entrada de SBOM por serviço para análise de cobertura.</summary>
    public sealed record ServiceSbomEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        bool CustomerFacing,
        int ComponentCount,
        int HighSeverityCveCount,
        int CriticalCveCount,
        int OutdatedComponentCount,
        IDictionary<string, int> LicenseDistribution,
        DateTimeOffset? LastSbomRecordedAt,
        IReadOnlyList<string> GplOrAgplComponents);
}
