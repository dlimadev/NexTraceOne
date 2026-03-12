using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Abstractions;

/// <summary>
/// Repositório de vínculos de usuário com tenants.
/// </summary>
public interface ITenantMembershipRepository
{
    /// <summary>Obtém um vínculo ativo ou inativo por usuário e tenant.</summary>
    Task<TenantMembership?> GetByUserAndTenantAsync(UserId userId, TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Lista os vínculos de um usuário.</summary>
    Task<IReadOnlyList<TenantMembership>> ListByUserAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Lista vínculos de um tenant com paginação e busca.</summary>
    Task<(IReadOnlyList<TenantMembership> Items, int TotalCount)> ListByTenantAsync(
        TenantId tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista todos os vínculos ativos de um tenant sem paginação.
    /// Usado em operações de recertificação de acessos (Access Review Campaign)
    /// onde é necessário processar todos os membros de uma vez.
    /// </summary>
    Task<IReadOnlyList<TenantMembership>> ListAllActiveByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo vínculo para persistência.</summary>
    void Add(TenantMembership membership);
}
