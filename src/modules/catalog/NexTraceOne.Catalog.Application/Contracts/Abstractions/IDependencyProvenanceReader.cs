namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de proveniência de dependências.
/// Agrega dados dos SbomRecord para análise de origem, licença e risco por componente.
/// Por omissão satisfeita por <c>NullDependencyProvenanceReader</c> (honest-null).
/// Wave AO.2 — GetDependencyProvenanceReport.
/// </summary>
public interface IDependencyProvenanceReader
{
    Task<IReadOnlyList<ComponentProvenanceEntry>> ListComponentsByTenantAsync(
        string tenantId,
        IReadOnlyList<string> approvedRegistries,
        IReadOnlyList<string> highRiskLicenses,
        int spofThreshold,
        CancellationToken ct);

    /// <summary>Entrada de proveniência por componente (agregado a nível de tenant).</summary>
    public sealed record ComponentProvenanceEntry(
        string ComponentName,
        IReadOnlyList<string> VersionsInUse,
        int ServiceCount,
        string RegistryOrigin,
        bool IsApprovedRegistry,
        string LicenseType,
        int TotalCveCount,
        string HighestSeverity);
}
