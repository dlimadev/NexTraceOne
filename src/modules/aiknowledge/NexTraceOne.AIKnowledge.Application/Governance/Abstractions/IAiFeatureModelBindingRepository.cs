using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de vinculações de funcionalidade a modelo de IA.
/// Suporta lookup por chave de feature/tenant, listagem e persistência.
/// </summary>
public interface IAiFeatureModelBindingRepository
{
    /// <summary>Obtém a vinculação ativa para uma chave de feature e tenant específicos.</summary>
    Task<AiFeatureModelBinding?> GetByFeatureKeyAsync(
        string featureKey,
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>Lista todas as vinculações de um tenant, com filtro opcional por estado ativo.</summary>
    Task<IReadOnlyList<AiFeatureModelBinding>> ListByTenantAsync(
        Guid tenantId,
        bool? isActive,
        CancellationToken ct = default);

    /// <summary>Obtém uma vinculação pelo identificador fortemente tipado.</summary>
    Task<AiFeatureModelBinding?> GetByIdAsync(
        AiFeatureModelBindingId id,
        CancellationToken ct = default);

    /// <summary>Verifica se já existe uma vinculação ativa para a mesma chave de feature no tenant.</summary>
    Task<bool> ExistsAsync(
        string featureKey,
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>Adiciona uma nova vinculação para persistência.</summary>
    Task AddAsync(AiFeatureModelBinding binding, CancellationToken ct = default);

    /// <summary>Atualiza uma vinculação existente.</summary>
    Task UpdateAsync(AiFeatureModelBinding binding, CancellationToken ct = default);
}
