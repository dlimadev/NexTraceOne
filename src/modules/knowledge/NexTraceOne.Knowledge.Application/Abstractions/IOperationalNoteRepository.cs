using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Application.Abstractions;

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
}
