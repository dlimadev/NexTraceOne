using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para comentários de negociações de contratos.
/// </summary>
public interface INegotiationCommentRepository
{
    /// <summary>Obtém um comentário pelo seu identificador.</summary>
    Task<NegotiationComment?> GetByIdAsync(NegotiationCommentId id, CancellationToken cancellationToken);

    /// <summary>Lista comentários de uma negociação específica.</summary>
    Task<IReadOnlyList<NegotiationComment>> ListByNegotiationIdAsync(Guid negotiationId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo comentário.</summary>
    Task AddAsync(NegotiationComment comment, CancellationToken cancellationToken);
}
