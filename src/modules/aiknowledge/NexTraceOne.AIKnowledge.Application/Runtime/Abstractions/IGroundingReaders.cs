namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

// ── Catalog grounding reader ────────────────────────────────────────────────

/// <summary>Contexto de um serviço obtido do Catálogo para grounding de IA.</summary>
public sealed record ServiceGroundingContext(
    string ServiceId,
    string DisplayName,
    string TeamName,
    string Domain,
    string Criticality,
    string Lifecycle,
    string ServiceType,
    string Description,
    string? SubDomain = null,
    string? Capability = null,
    string? DataClassification = null,
    string? RegulatoryScope = null,
    string? SloTarget = null,
    string? ProductOwner = null,
    string? ContactChannel = null);

/// <summary>
/// Leitor somente-leitura de contexto de serviços do Catálogo para grounding de IA.
/// Abstrai o acesso cross-módulo ao CatalogGraphDbContext.
/// </summary>
public interface ICatalogGroundingReader
{
    /// <summary>
    /// Retorna os serviços que correspondem ao serviceId ou ao searchTerm (até maxResults).
    /// </summary>
    Task<IReadOnlyList<ServiceGroundingContext>> FindServicesAsync(
        string? serviceId,
        string searchTerm,
        int maxResults,
        CancellationToken ct = default);
}

// ── ChangeIntelligence grounding reader ────────────────────────────────────

/// <summary>Contexto de uma release para grounding de IA.</summary>
public sealed record ReleaseGroundingContext(
    string ReleaseId,
    string ServiceName,
    string Version,
    string Environment,
    string Status,
    string ChangeLevel,
    decimal ChangeScore,
    string? Description,
    DateTimeOffset CreatedAt);

/// <summary>
/// Leitor somente-leitura de releases para grounding de IA.
/// Abstrai o acesso cross-módulo ao ChangeIntelligenceDbContext.
/// </summary>
public interface IChangeGroundingReader
{
    /// <summary>
    /// Retorna releases recentes dentro da janela temporal, opcionalmente filtradas por serviço, ambiente e tenant.
    /// </summary>
    Task<IReadOnlyList<ReleaseGroundingContext>> FindRecentReleasesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? serviceId,
        string? environment,
        Guid? tenantId,
        int maxResults,
        CancellationToken ct = default);
}

// ── Incident grounding reader ───────────────────────────────────────────────

/// <summary>Contexto de um incidente para grounding de IA.</summary>
public sealed record IncidentGroundingContext(
    string IncidentId,
    string Title,
    string ServiceName,
    string Severity,
    string Status,
    string Environment,
    string? Description,
    DateTimeOffset DetectedAt);

/// <summary>
/// Leitor somente-leitura de incidentes para grounding de IA.
/// Abstrai o acesso cross-módulo ao IncidentDbContext.
/// </summary>
public interface IIncidentGroundingReader
{
    /// <summary>
    /// Retorna incidentes recentes dentro da janela temporal, opcionalmente filtrados por serviço e ambiente.
    /// </summary>
    Task<IReadOnlyList<IncidentGroundingContext>> FindRecentIncidentsAsync(
        DateTimeOffset from,
        string? serviceId,
        string? environment,
        int maxResults,
        CancellationToken ct = default);
}

// ── Knowledge Hub document grounding reader ────────────────────────────────

/// <summary>Contexto de um documento de conhecimento para grounding de IA.</summary>
public sealed record KnowledgeDocumentGroundingContext(
    string DocumentId,
    string Title,
    string? Summary,
    string Category);

/// <summary>
/// Leitor somente-leitura de documentos do Knowledge Hub para grounding de IA.
/// Abstrai o acesso cross-módulo ao KnowledgeDbContext.
/// </summary>
public interface IKnowledgeDocumentGroundingReader
{
    /// <summary>
    /// Pesquisa documentos por termo textual no título, sumário e conteúdo.
    /// </summary>
    Task<IReadOnlyList<KnowledgeDocumentGroundingContext>> SearchDocumentsAsync(
        string searchTerm,
        int maxResults,
        CancellationToken ct = default);
}

// ── Contract grounding reader ───────────────────────────────────────────────

/// <summary>Contexto de uma versão de contrato para grounding de IA.</summary>
public sealed record ContractGroundingContext(
    string ContractVersionId,
    string ApiAssetId,
    string Version,
    string Protocol,
    string LifecycleState,
    bool IsLocked,
    DateTimeOffset? LockedAt);

/// <summary>
/// Leitor somente-leitura de versões de contrato para grounding de IA.
/// Abstrai o acesso cross-módulo ao ContractsDbContext.
/// </summary>
public interface IContractGroundingReader
{
    /// <summary>
    /// Retorna versões de contrato filtrando opcionalmente por ID de versão, ID de API asset
    /// e estado de ciclo de vida; pesquisa textual por semver quando searchTerm for fornecido.
    /// </summary>
    Task<IReadOnlyList<ContractGroundingContext>> FindContractVersionsAsync(
        Guid? contractVersionId,
        Guid? apiAssetId,
        string? searchTerm,
        int maxResults,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna versões de contrato vinculadas a uma interface de serviço via ContractBinding,
    /// opcionalmente filtradas por ambiente. Navega ServiceInterface → ContractBinding → ContractVersion.
    /// </summary>
    Task<IReadOnlyList<ContractGroundingContext>> FindContractsByServiceInterfaceAsync(
        Guid serviceInterfaceId,
        string? environment,
        int maxResults,
        CancellationToken ct = default);
}

// ── ServiceInterface grounding reader ──────────────────────────────────────

/// <summary>Contexto de uma interface de serviço para grounding de IA.</summary>
public sealed record ServiceInterfaceGroundingContext(
    string InterfaceId,
    string ServiceAssetId,
    string ServiceName,
    string Name,
    string Description,
    string InterfaceType,
    string Status,
    string ExposureScope,
    string? SloTarget,
    bool RequiresContract,
    string AuthScheme,
    DateTimeOffset? DeprecationDate);

/// <summary>
/// Leitor somente-leitura de interfaces de serviço do Catálogo para grounding de IA.
/// Abstrai o acesso cross-módulo ao CatalogGraphDbContext para ServiceInterface e ContractBinding.
/// </summary>
public interface IServiceInterfaceGroundingReader
{
    /// <summary>
    /// Retorna interfaces de serviço pertencentes a um serviço (por nome ou ID).
    /// </summary>
    Task<IReadOnlyList<ServiceInterfaceGroundingContext>> FindInterfacesByServiceAsync(
        string serviceIdentifier,
        int maxResults,
        CancellationToken ct = default);

    /// <summary>
    /// Pesquisa interfaces de serviço por nome ou descrição.
    /// </summary>
    Task<IReadOnlyList<ServiceInterfaceGroundingContext>> FindInterfacesByNameAsync(
        string searchTerm,
        int maxResults,
        CancellationToken ct = default);
}
