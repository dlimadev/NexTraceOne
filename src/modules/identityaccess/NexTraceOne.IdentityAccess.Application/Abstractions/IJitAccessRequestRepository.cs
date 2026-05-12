using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório para gestão de requests de acesso JIT (Just-In-Time).
/// </summary>
public interface IJitAccessRequestRepository
{
    /// <summary>Adiciona um novo request JIT para persistência.</summary>
    Task AddAsync(JitAccessRequest request, CancellationToken cancellationToken);

    /// <summary>Lista requests pendentes por utilizador e tenant.</summary>
    Task<IReadOnlyList<JitAccessRequest>> ListPendingByUserAsync(
        UserId userId, TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Lista requests activos (aprovados e não expirados) por utilizador.</summary>
    Task<IReadOnlyList<JitAccessRequest>> ListActiveByUserAsync(
        UserId userId, TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Obtém um request pelo identificador.</summary>
    Task<JitAccessRequest?> GetByIdAsync(JitAccessRequestId id, CancellationToken cancellationToken);
}
