using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Interface do repositório de Notebooks para o módulo Governance.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public interface INotebookRepository
{
    /// <summary>Lista notebooks do tenant com filtros opcionais.</summary>
    Task<IReadOnlyList<Notebook>> ListAsync(
        string tenantId,
        string? persona,
        NotebookStatus? status,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Conta notebooks do tenant.</summary>
    Task<int> CountAsync(string tenantId, string? persona, NotebookStatus? status, CancellationToken ct);

    /// <summary>Obtém uma notebook pelo identificador e tenant.</summary>
    Task<Notebook?> GetByIdAsync(NotebookId id, string tenantId, CancellationToken ct);

    /// <summary>Adiciona uma nova notebook.</summary>
    Task AddAsync(Notebook notebook, CancellationToken ct);

    /// <summary>Atualiza uma notebook existente.</summary>
    Task UpdateAsync(Notebook notebook, CancellationToken ct);

    /// <summary>Remove uma notebook do repositório.</summary>
    Task DeleteAsync(Notebook notebook, CancellationToken ct);
}
