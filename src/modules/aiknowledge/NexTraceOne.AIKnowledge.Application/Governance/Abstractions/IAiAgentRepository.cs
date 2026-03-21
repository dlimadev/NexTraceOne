using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de agents de IA registados na plataforma.
/// Suporta listagem filtrada por categoria e estado de ativação.
/// </summary>
public interface IAiAgentRepository
{
    /// <summary>Lista agents com filtros opcionais.</summary>
    Task<IReadOnlyList<AiAgent>> ListAsync(
        bool? isActive,
        bool? isOfficial,
        CancellationToken ct);

    /// <summary>Lista agents filtrando por categorias específicas.</summary>
    Task<IReadOnlyList<AiAgent>> ListByCategoriesAsync(
        IReadOnlyList<AgentCategory> categories,
        bool? isActive,
        CancellationToken ct);

    /// <summary>Obtém um agent pelo identificador fortemente tipado.</summary>
    Task<AiAgent?> GetByIdAsync(AiAgentId id, CancellationToken ct);

    /// <summary>Adiciona um novo agent para persistência.</summary>
    Task AddAsync(AiAgent agent, CancellationToken ct);

    /// <summary>Atualiza um agent existente.</summary>
    Task UpdateAsync(AiAgent agent, CancellationToken ct);
}
