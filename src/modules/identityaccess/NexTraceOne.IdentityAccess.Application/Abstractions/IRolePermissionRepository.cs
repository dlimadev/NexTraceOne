using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório para mapeamentos papel→permissão persistidos em base de dados.
/// Permite consulta de permissões por papel com suporte a personalização por tenant.
///
/// Resolução de prioridade:
/// 1. Mapeamentos específicos do tenant (TenantId preenchido) têm precedência.
/// 2. Mapeamentos do sistema (TenantId nulo) são usados como fallback.
/// 3. <see cref="RolePermissionCatalog"/> é usado como último recurso quando a
///    tabela ainda não foi populada (compatibilidade retroativa).
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// Obtém os códigos de permissão ativos para um papel, considerando o tenant.
    /// Retorna permissões do tenant se existirem, caso contrário retorna as do sistema.
    /// </summary>
    /// <param name="roleId">Identificador do papel.</param>
    /// <param name="tenantId">Identificador do tenant (pode ser nulo para sistema).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de códigos de permissão ativos.</returns>
    Task<IReadOnlyList<string>> GetPermissionCodesForRoleAsync(
        RoleId roleId,
        TenantId? tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se existem mapeamentos persistidos para um papel específico.
    /// Utilizado para determinar se deve usar fallback para <see cref="RolePermissionCatalog"/>.
    /// </summary>
    Task<bool> HasMappingsForRoleAsync(RoleId roleId, TenantId? tenantId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo mapeamento papel→permissão.</summary>
    Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken);

    /// <summary>Adiciona vários mapeamentos papel→permissão em lote.</summary>
    Task AddRangeAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken);
}
