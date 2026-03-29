using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Serviço de resolução de permissões com estratégia DB-first e fallback estático.
///
/// Prioridade de resolução:
/// 1. Mapeamentos persistidos em base de dados (iam_role_permissions) para o par role+tenant.
/// 2. Fallback para <see cref="RolePermissionCatalog"/> quando a tabela ainda não foi populada.
///
/// Esta abstração permite que o produto evolua para personalização de permissões por tenant
/// mantendo compatibilidade retroativa com o catálogo estático.
/// </summary>
public interface IPermissionResolver
{
    /// <summary>
    /// Resolve as permissões efetivas para um papel, considerando tenant e fallback estático.
    /// </summary>
    /// <param name="roleId">Identificador do papel.</param>
    /// <param name="roleName">Nome do papel, usado para fallback ao catálogo estático.</param>
    /// <param name="tenantId">Tenant para resolução contextual; nulo para padrões de sistema.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista imutável de códigos de permissão no formato módulo:recurso:ação.</returns>
    Task<IReadOnlyList<string>> ResolvePermissionsAsync(
        RoleId roleId,
        string roleName,
        TenantId? tenantId,
        CancellationToken cancellationToken);
}
