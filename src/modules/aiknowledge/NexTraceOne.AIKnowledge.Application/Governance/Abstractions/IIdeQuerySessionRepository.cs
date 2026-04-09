using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de sessões de consulta IDE governadas.
/// Suporta consulta por utilizador, cliente IDE e estado para auditoria e análise de uso.
/// </summary>
public interface IIdeQuerySessionRepository
{
    /// <summary>Adiciona uma nova sessão de consulta IDE para persistência.</summary>
    Task AddAsync(IdeQuerySession session, CancellationToken ct = default);

    /// <summary>Obtém uma sessão pelo identificador.</summary>
    Task<IdeQuerySession?> GetByIdAsync(IdeQuerySessionId id, CancellationToken ct = default);

    /// <summary>Lista sessões de consulta IDE com filtros opcionais.</summary>
    Task<IReadOnlyList<IdeQuerySession>> ListAsync(
        string? userId,
        string? ideClient,
        IdeQuerySessionStatus? status,
        CancellationToken ct = default);

    /// <summary>Atualiza uma sessão de consulta IDE existente.</summary>
    Task UpdateAsync(IdeQuerySession session, CancellationToken ct = default);
}
