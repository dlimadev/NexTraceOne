using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de delegações formais de permissões entre usuários.
/// </summary>
public interface IDelegationRepository
{
    /// <summary>Obtém uma delegação pelo identificador.</summary>
    Task<Delegation?> GetByIdAsync(DelegationId id, CancellationToken cancellationToken);

    /// <summary>Lista delegações ativas onde o usuário é o delegatário (recebedor).</summary>
    Task<IReadOnlyList<Delegation>> ListActiveByDelegateeAsync(UserId delegateeId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Lista delegações criadas por um delegante específico.</summary>
    Task<IReadOnlyList<Delegation>> ListByGrantorAsync(UserId grantorId, CancellationToken cancellationToken);

    /// <summary>Lista todas as delegações ativas no tenant.</summary>
    Task<IReadOnlyList<Delegation>> ListActiveByTenantAsync(TenantId tenantId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Lista todas as delegações do tenant para histórico investigativo.</summary>
    Task<IReadOnlyList<Delegation>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova delegação para persistência.</summary>
    void Add(Delegation delegation);
}
