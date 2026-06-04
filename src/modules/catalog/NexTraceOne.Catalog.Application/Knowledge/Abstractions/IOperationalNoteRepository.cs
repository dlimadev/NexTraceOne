using NexTraceOne.Catalog.Domain.Knowledge.Entities;
using NexTraceOne.Catalog.Domain.Knowledge.Enums;

namespace NexTraceOne.Catalog.Application.Knowledge.Abstractions;

/// <summary>
/// Repositório de OperationalNote.
/// </summary>
public interface IOperationalNoteRepository
{
    /// <summary>Obtém uma nota pelo identificador.</summary>
    Task<OperationalNote?> GetByIdAsync(OperationalNoteId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova nota.</summary>
    Task AddAsync(OperationalNote note, CancellationToken cancellationToken = default);

    /// <summary>Atualiza uma nota existente.</summary>
    void Update(OperationalNote note);

    /// <summary>Pesquisa notas operacionais por termo textual (título, conteúdo).</summary>
    Task<IReadOnlyList<OperationalNote>> SearchAsync(string searchTerm, int maxResults, CancellationToken cancellationToken = default);

    /// <summary>Lista notas com paginação e filtros opcionais (severidade, contextType, contextEntityId).</summary>
    Task<(IReadOnlyList<OperationalNote> Items, int TotalCount)> ListAsync(
        NoteSeverity? severity,
        string? contextType,
        Guid? contextEntityId,
        bool? isResolved,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
