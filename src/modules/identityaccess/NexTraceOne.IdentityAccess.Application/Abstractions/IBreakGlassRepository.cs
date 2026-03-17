using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de solicitações Break Glass (acesso emergencial).
/// </summary>
public interface IBreakGlassRepository
{
    /// <summary>Obtém uma solicitação pelo identificador.</summary>
    Task<BreakGlassRequest?> GetByIdAsync(BreakGlassRequestId id, CancellationToken cancellationToken);

    /// <summary>
    /// Conta o número de usos de Break Glass de um usuário no trimestre atual.
    /// Utilizado para validação do limite trimestral antes de conceder novo acesso.
    /// </summary>
    Task<int> CountQuarterlyUsageAsync(UserId userId, DateTimeOffset quarterStart, CancellationToken cancellationToken);

    /// <summary>Lista solicitações ativas no tenant.</summary>
    Task<IReadOnlyList<BreakGlassRequest>> ListActiveByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Lista solicitações com post-mortem pendente.</summary>
    Task<IReadOnlyList<BreakGlassRequest>> ListPendingPostMortemAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova solicitação para persistência.</summary>
    void Add(BreakGlassRequest request);
}
