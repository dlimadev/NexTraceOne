namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório de SbomRecord — regista o Bill of Materials (SBOM) de um serviço
/// para análise de supply chain, vulnerabilidades e proveniência de dependências.
/// Implementação EF Core em Infrastructure/Contracts/Persistence/Repositories/EfSbomRepository.
/// Wave AO.1 — Supply Chain &amp; Dependency Provenance.
/// </summary>
public interface ISbomRepository
{
    Task AddAsync(SbomRecord record, CancellationToken ct);

    Task<SbomRecord?> GetLatestAsync(string serviceId, string tenantId, CancellationToken ct);

    Task<IReadOnlyList<SbomRecord>> ListByTenantAsync(string tenantId, CancellationToken ct);
}

/// <summary>
/// Entidade de domínio que representa o Software Bill of Materials (SBOM)
/// de um serviço numa versão específica.
/// Wave AO.1 — Supply Chain &amp; Dependency Provenance.
/// </summary>
public sealed record SbomRecord(
    Guid Id,
    string TenantId,
    string ServiceId,
    string ServiceName,
    string Version,
    DateTimeOffset RecordedAt,
    IReadOnlyList<SbomComponent> Components)
{
    // EF Core requires a parameterless constructor for materialization
    private SbomRecord() : this(
        Guid.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        DateTimeOffset.MinValue,
        new List<SbomComponent>())
    {
    }
}

/// <summary>
/// Componente individual do SBOM (dependência directa ou transitiva).
/// </summary>
public sealed record SbomComponent(
    string Name,
    string Version,
    string Registry,
    string License,
    int CveCount,
    string HighestCveSeverity)
{
    // EF Core requires a parameterless constructor for materialization
    private SbomComponent() : this(string.Empty, string.Empty, string.Empty, string.Empty, 0, string.Empty)
    {
    }
}
