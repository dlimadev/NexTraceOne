using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de solicitações de acesso privilegiado temporário (JIT).
/// </summary>
public interface IJitAccessRepository
{
    /// <summary>Obtém uma solicitação pelo identificador.</summary>
    Task<JitAccessRequest?> GetByIdAsync(JitAccessRequestId id, CancellationToken cancellationToken);

    /// <summary>Lista solicitações pendentes de aprovação no tenant.</summary>
    Task<IReadOnlyList<JitAccessRequest>> ListPendingByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Lista solicitações JIT de um tenant para histórico investigativo.</summary>
    Task<IReadOnlyList<JitAccessRequest>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Lista solicitações de um usuário específico.</summary>
    Task<IReadOnlyList<JitAccessRequest>> ListByUserAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se o usuário tem acesso JIT ativo para uma permissão específica.
    /// Utilizado pelo mecanismo de autorização para considerar permissões temporárias.
    /// </summary>
    Task<bool> HasActiveGrantAsync(UserId userId, string permissionCode, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova solicitação para persistência.</summary>
    void Add(JitAccessRequest request);
}
