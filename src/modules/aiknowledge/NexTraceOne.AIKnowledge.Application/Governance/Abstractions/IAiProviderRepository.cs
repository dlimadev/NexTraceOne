using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de provedores de IA registados na plataforma.
/// Suporta consulta individual, listagem completa, filtragem por estado e persistência.
/// </summary>
public interface IAiProviderRepository
{
    /// <summary>Obtém um provedor pelo identificador fortemente tipado.</summary>
    Task<AiProvider?> GetByIdAsync(AiProviderId id, CancellationToken ct = default);

    /// <summary>Lista todos os provedores registados.</summary>
    Task<IReadOnlyList<AiProvider>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Lista apenas os provedores ativos (IsEnabled = true).</summary>
    Task<IReadOnlyList<AiProvider>> GetEnabledAsync(CancellationToken ct = default);

    /// <summary>Adiciona um novo provedor para persistência.</summary>
    Task AddAsync(AiProvider entity, CancellationToken ct = default);
}
