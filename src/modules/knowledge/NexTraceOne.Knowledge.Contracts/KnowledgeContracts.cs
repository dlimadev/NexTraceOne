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
