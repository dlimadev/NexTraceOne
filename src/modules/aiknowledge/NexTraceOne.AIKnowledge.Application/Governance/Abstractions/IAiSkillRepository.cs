using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de skills de IA registadas na plataforma.
/// Suporta listagem filtrada por estado, tipo de ownership e tenant.
/// </summary>
public interface IAiSkillRepository
{
    /// <summary>Obtém uma skill pelo identificador fortemente tipado.</summary>
    Task<AiSkill?> GetByIdAsync(AiSkillId id, CancellationToken ct);

    /// <summary>Obtém uma skill pelo nome dentro de um tenant.</summary>
    Task<AiSkill?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct);

    /// <summary>Lista skills com filtros opcionais.</summary>
    Task<IReadOnlyList<AiSkill>> ListAsync(
        SkillStatus? status,
        SkillOwnershipType? ownershipType,
        Guid? tenantId,
        CancellationToken ct);

    /// <summary>Verifica se já existe uma skill com o nome especificado para o tenant.</summary>
    Task<bool> ExistsByNameAsync(string name, Guid tenantId, CancellationToken ct);

    /// <summary>Adiciona uma nova skill para persistência.</summary>
    void Add(AiSkill skill);

    /// <summary>Conta execuções de uma skill (para métricas).</summary>
    Task<int> CountBySkillIdAsync(AiSkillId id, CancellationToken ct);
}
