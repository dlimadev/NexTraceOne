using NexTraceOne.Knowledge.Domain.Entities;

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
}
