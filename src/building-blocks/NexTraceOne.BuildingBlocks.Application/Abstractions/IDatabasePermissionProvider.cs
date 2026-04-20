namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para resolução de permissões persistidas em base de dados.
/// Permite que o handler de autorização consulte mapeamentos papel→permissão
/// armazenados no banco, com suporte a personalização por tenant,
/// sem depender diretamente do módulo IdentityAccess.
///
/// Modelo enterprise (bancos/seguradoras): permissões são armazenadas em base de dados
/// para permitir customização granular por tenant, sem necessidade de redeploy.
/// O <c>RolePermissionCatalog</c> estático permanece como fallback para cenários
/// onde a tabela ainda não foi populada.
/// </summary>
public interface IDatabasePermissionProvider
{
    /// <summary>
    /// Verifica se o utilizador possui uma permissão específica via mapeamento
    /// papel→permissão persistido no banco de dados.
    /// </summary>
    /// <param name="userId">Identificador do utilizador.</param>
    /// <param name="roleId">Identificador do papel do utilizador (claim role_id do JWT).</param>
    /// <param name="tenantId">Identificador do tenant (claim tenant_id do JWT).</param>
    /// <param name="permissionCode">Código da permissão requerida.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a permissão está concedida no banco para este papel/tenant.</returns>
    Task<bool> HasPermissionAsync(
        string userId,
        string roleId,
        string tenantId,
        string permissionCode,
        CancellationToken cancellationToken);
}
