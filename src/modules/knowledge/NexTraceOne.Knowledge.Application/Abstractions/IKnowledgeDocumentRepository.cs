using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Abstractions;

/// <summary>
/// Repositório de KnowledgeDocument.
/// </summary>
public interface IKnowledgeDocumentRepository
{
    /// <summary>Obtém um documento pelo identificador.</summary>
    Task<KnowledgeDocument?> GetByIdAsync(KnowledgeDocumentId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo documento.</summary>
    Task AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);

    /// <summary>Atualiza um documento existente.</summary>
    void Update(KnowledgeDocument document);

    /// <summary>Pesquisa documentos por termo textual (título, conteúdo, summary, tags).</summary>
    Task<IReadOnlyList<KnowledgeDocument>> SearchAsync(string searchTerm, int maxResults, CancellationToken cancellationToken = default);

    /// <summary>Lista documentos com paginação e filtro opcional por categoria/status.</summary>
    Task<(IReadOnlyList<KnowledgeDocument> Items, int TotalCount)> ListAsync(
        DocumentCategory? category,
        DocumentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém um documento pelo slug.</summary>
    Task<KnowledgeDocument?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
