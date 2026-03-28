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
    string Description);

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
