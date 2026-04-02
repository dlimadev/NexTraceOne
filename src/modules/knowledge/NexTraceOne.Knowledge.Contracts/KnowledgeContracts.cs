namespace NexTraceOne.Knowledge.Contracts;

/// <summary>
/// Contratos públicos do módulo Knowledge para comunicação entre módulos.
/// Integration Events e DTOs partilhados.
/// </summary>
public static class KnowledgeContracts
{
    /// <summary>Nome do módulo para identificação em contratos.</summary>
    public const string ModuleName = "Knowledge";
}

// IMPLEMENTATION STATUS: Implemented — KnowledgeModuleService (Infrastructure).

/// <summary>
/// Interface pública do módulo Knowledge para comunicação entre módulos.
/// Outros módulos que precisarem de dados de conhecimento devem usar este contrato —
/// nunca acessar o DbContext ou repositórios diretamente.
/// Garante isolamento de base de dados entre módulos.
/// </summary>
public interface IKnowledgeModule
{
    /// <summary>
    /// Conta o total de documentos de conhecimento registados.
    /// </summary>
    Task<int> CountDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta o total de notas operacionais registadas.
    /// </summary>
    Task<int> CountOperationalNotesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta documentos de conhecimento associados a um serviço específico.
    /// </summary>
    Task<int> CountDocumentsByServiceAsync(string serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um resumo do módulo Knowledge para consumo cross-module.
    /// </summary>
    Task<KnowledgeModuleSummary> GetModuleSummaryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Resumo do módulo Knowledge para consumo cross-module.
/// Contém contagens de documentos e notas para dashboards de governança.
/// </summary>
public sealed record KnowledgeModuleSummary(
    int TotalDocuments,
    int TotalOperationalNotes,
    int TotalRelations);

/// <summary>
/// Item de resultado de pesquisa do módulo Knowledge para consumo cross-module.
/// Usado pelo GlobalSearch para incluir documentos de conhecimento e notas operacionais
/// nos resultados unificados da pesquisa.
/// </summary>
public sealed record KnowledgeSearchResultItem(
    Guid EntityId,
    string EntityType,
    string Title,
    string? Subtitle,
    string? Status,
    string Route,
    double RelevanceScore);

/// <summary>
/// Contrato cross-module para pesquisa no Knowledge Hub.
/// Permite que outros módulos (ex: Catalog GlobalSearch) consultem
/// documentos de conhecimento e notas operacionais sem dependência directa
/// do domínio ou infraestrutura do módulo Knowledge.
///
/// P10.2: Motor inicial de search cross-module com PostgreSQL ILIKE.
/// </summary>
public interface IKnowledgeSearchProvider
{
    /// <summary>
    /// Pesquisa documentos de conhecimento e notas operacionais por termo textual.
    /// Retorna resultados unificados com relevância calculada.
    /// </summary>
    Task<IReadOnlyList<KnowledgeSearchResultItem>> SearchAsync(
        string searchTerm,
        string? scope,
        int maxResults,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contrato cross-module para ligar runbooks operacionais ao Knowledge Hub.
/// A implementação cria relações e notas operacionais para grounding contextual.
/// </summary>
public interface IRunbookKnowledgeLinkingService
{
    /// <summary>
    /// Garante ligação idempotente de um runbook com o serviço associado no Knowledge Hub.
    /// Quando aplicável, cria nota operacional de resumo para pesquisa/grounding.
    /// </summary>
    Task LinkRunbookToServiceAsync(
        Guid runbookId,
        string runbookTitle,
        string runbookDescription,
        string? linkedServiceId,
        string maintainedBy,
        CancellationToken cancellationToken = default);
}
