using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de políticas de acesso por ambiente.
/// </summary>
public interface IEnvironmentAccessPolicyRepository
{
    /// <summary>Obtém uma política pelo identificador.</summary>
    Task<EnvironmentAccessPolicy?> GetByIdAsync(EnvironmentAccessPolicyId id, CancellationToken cancellationToken);

    /// <summary>Lista todas as políticas activas de um tenant.</summary>
    Task<IReadOnlyList<EnvironmentAccessPolicy>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova política para persistência.</summary>
    Task AddAsync(EnvironmentAccessPolicy policy, CancellationToken cancellationToken);

    /// <summary>Marca a política como alterada no contexto EF.</summary>
    Task UpdateAsync(EnvironmentAccessPolicy policy, CancellationToken cancellationToken);
}
