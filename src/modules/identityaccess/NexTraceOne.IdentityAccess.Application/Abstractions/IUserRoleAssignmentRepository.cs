using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório para gestão de atribuições de papéis a usuários em tenants.
///
/// Suporta o modelo multi-role: um usuário pode ter N papéis no mesmo tenant.
/// As permissões efetivas são a UNIÃO de todos os papéis ativos.
///
/// Consultas consideram vigência temporal (ValidFrom/ValidUntil) e flag IsActive.
/// </summary>
public interface IUserRoleAssignmentRepository
{
    /// <summary>
    /// Obtém todas as atribuições ativas de um usuário em um tenant específico.
    /// Considera vigência temporal (ValidFrom/ValidUntil).
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="now">Data/hora UTC atual para validação de vigência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de atribuições efetivamente ativas.</returns>
    Task<IReadOnlyList<UserRoleAssignment>> GetActiveAssignmentsAsync(
        UserId userId,
        TenantId tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtém todas as atribuições de um usuário (todos os tenants).
    /// </summary>
    Task<IReadOnlyList<UserRoleAssignment>> ListByUserAsync(
        UserId userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista todas as atribuições ativas de um tenant com paginação.
    /// </summary>
    Task<(IReadOnlyList<UserRoleAssignment> Items, int TotalCount)> ListByTenantAsync(
        TenantId tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se existe uma atribuição ativa do mesmo papel para o usuário/tenant.
    /// Usada para evitar duplicatas antes de criar nova atribuição.
    /// </summary>
    Task<bool> ExistsAsync(
        UserId userId,
        TenantId tenantId,
        RoleId roleId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma atribuição específica pelo identificador.
    /// </summary>
    Task<UserRoleAssignment?> GetByIdAsync(
        UserRoleAssignmentId id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista atribuições expiradas que precisam de limpeza ou notificação.
    /// </summary>
    Task<IReadOnlyList<UserRoleAssignment>> ListExpiredAssignmentsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova atribuição.</summary>
    void Add(UserRoleAssignment assignment);
}
