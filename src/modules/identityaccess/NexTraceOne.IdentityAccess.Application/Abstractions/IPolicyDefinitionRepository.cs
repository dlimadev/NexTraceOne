using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>Repositório de definições de políticas do Policy Studio.</summary>
public interface IPolicyDefinitionRepository
{
    /// <summary>Obtém uma definição de política pelo identificador.</summary>
    Task<PolicyDefinition?> GetByIdAsync(PolicyDefinitionId id, CancellationToken ct = default);

    /// <summary>Lista definições de políticas de um tenant, com filtro opcional por tipo.</summary>
    Task<IReadOnlyList<PolicyDefinition>> ListByTenantAsync(string tenantId, PolicyDefinitionType? type = null, CancellationToken ct = default);

    /// <summary>Lista definições de políticas activas por tipo (para avaliação em runtime).</summary>
    Task<IReadOnlyList<PolicyDefinition>> ListEnabledByTypeAsync(PolicyDefinitionType type, CancellationToken ct = default);

    /// <summary>Adiciona uma nova definição de política.</summary>
    void Add(PolicyDefinition policy);

    /// <summary>Marca a definição de política como modificada.</summary>
    void Update(PolicyDefinition policy);
}
