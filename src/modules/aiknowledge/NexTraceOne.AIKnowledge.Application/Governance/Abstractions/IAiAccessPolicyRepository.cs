using NexTraceOne.AiGovernance.Domain.Entities;

namespace NexTraceOne.AiGovernance.Application.Abstractions;

/// <summary>
/// Repositório de políticas de acesso de IA.
/// Suporta listagem filtrada por escopo e estado de ativação.
/// </summary>
public interface IAiAccessPolicyRepository
{
    /// <summary>Lista políticas com filtros opcionais de escopo e estado ativo.</summary>
    Task<IReadOnlyList<AIAccessPolicy>> ListAsync(
        string? scope,
        bool? isActive,
        CancellationToken ct);

    /// <summary>Obtém uma política pelo identificador fortemente tipado.</summary>
    Task<AIAccessPolicy?> GetByIdAsync(AIAccessPolicyId id, CancellationToken ct);

    /// <summary>Adiciona uma nova política para persistência.</summary>
    Task AddAsync(AIAccessPolicy policy, CancellationToken ct);

    /// <summary>Atualiza uma política existente.</summary>
    Task UpdateAsync(AIAccessPolicy policy, CancellationToken ct);
}
