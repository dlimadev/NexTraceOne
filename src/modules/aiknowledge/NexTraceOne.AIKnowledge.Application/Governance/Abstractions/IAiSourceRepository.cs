using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de fontes de dados registadas para grounding e retrieval de IA.
/// Suporta consulta individual, listagem, filtragem por tipo e estado, e persistência.
/// </summary>
public interface IAiSourceRepository
{
    /// <summary>Obtém uma fonte pelo identificador fortemente tipado.</summary>
    Task<AiSource?> GetByIdAsync(AiSourceId id, CancellationToken ct = default);

    /// <summary>Lista todas as fontes registadas.</summary>
    Task<IReadOnlyList<AiSource>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Lista fontes filtradas por tipo.</summary>
    Task<IReadOnlyList<AiSource>> GetByTypeAsync(AiSourceType sourceType, CancellationToken ct = default);

    /// <summary>Lista apenas as fontes ativas (IsEnabled = true).</summary>
    Task<IReadOnlyList<AiSource>> GetEnabledAsync(CancellationToken ct = default);

    /// <summary>Adiciona uma nova fonte para persistência.</summary>
    Task AddAsync(AiSource entity, CancellationToken ct = default);
}
