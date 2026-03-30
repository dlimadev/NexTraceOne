namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para resolução de autorização via políticas de acesso ao nível
/// de módulo/página/ação, persistidas em base de dados.
///
/// Permite que o <see cref="PermissionAuthorizationHandler"/> consulte a tabela
/// iam_module_access_policies como fonte adicional de autorização, complementando
/// os mapeamentos papel→permissão planos da tabela iam_role_permissions.
///
/// Modelo enterprise (módulo/página/ação):
/// - Suporta wildcard ("*") para página e ação.
/// - Prioridade: políticas tenant-specific > políticas de sistema.
/// - Deny explícito: IsAllowed=false bloqueia acesso mesmo que outra fonte conceda.
/// </summary>
public interface IModuleAccessPermissionProvider
{
    /// <summary>
    /// Verifica se o utilizador tem acesso permitido pela política de módulo/página/ação
    /// para um dado código de permissão no formato "módulo:recurso:ação".
    ///
    /// O provider traduz internamente o permission code plano para o triplo
    /// (Module, Page, Action) e consulta as políticas persistidas.
    /// </summary>
    /// <param name="userId">Identificador do utilizador.</param>
    /// <param name="roleId">Identificador do papel (claim role_id ou role_ids do JWT).</param>
    /// <param name="tenantId">Identificador do tenant (claim tenant_id do JWT).</param>
    /// <param name="permissionCode">Código da permissão no formato "módulo:recurso:ação".</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>
    /// <c>true</c> se uma política activa concede acesso;
    /// <c>false</c> se uma política activa nega acesso explicitamente;
    /// <c>null</c> se nenhuma política se aplica (fallback para próxima fonte na cascata).
    /// </returns>
    Task<bool?> HasModuleAccessAsync(
        string userId,
        string roleId,
        string tenantId,
        string permissionCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifica acesso usando directamente o triplo módulo/página/ação,
    /// sem necessidade de tradução a partir de permission code plano.
    /// Usado por endpoints que adoptam o novo modelo <c>RequireModuleAccess</c>.
    /// </summary>
    /// <param name="userId">Identificador do utilizador.</param>
    /// <param name="roleId">Identificador do papel.</param>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="module">Módulo da plataforma (ex.: "AI", "Catalog").</param>
    /// <param name="page">Página ou sub-área (ex.: "Runtime", "ServiceCatalog"). Use "*" para wildcard.</param>
    /// <param name="action">Ação granular (ex.: "Read", "Write", "Approve"). Use "*" para wildcard.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>
    /// <c>true</c> se permitido; <c>false</c> se negado; <c>null</c> se nenhuma política se aplica.
    /// </returns>
    Task<bool?> HasModuleAccessDirectAsync(
        string userId,
        string roleId,
        string tenantId,
        string module,
        string page,
        string action,
        CancellationToken cancellationToken);
}
