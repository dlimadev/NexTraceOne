using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório para consulta de mapeamentos de grupos SSO para roles internos.
/// Utilizado durante autenticação federada para derivar automaticamente o role
/// do utilizador a partir dos seus grupos no provedor externo.
/// </summary>
public interface ISsoGroupMappingRepository
{
    /// <summary>
    /// Obtém o primeiro mapeamento activo que corresponde a um dos grupos externos fornecidos,
    /// para o provedor e tenant especificados.
    /// Retorna <c>null</c> se nenhum mapeamento activo for encontrado.
    /// </summary>
    /// <param name="tenantId">Tenant onde procurar mapeamentos.</param>
    /// <param name="provider">Nome do provedor SSO (e.g., "AzureAD", "Okta").</param>
    /// <param name="externalGroupIds">Identificadores dos grupos do utilizador no provedor externo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<SsoGroupMapping?> FindActiveByGroupsAsync(
        TenantId tenantId,
        string provider,
        IReadOnlyCollection<string> externalGroupIds,
        CancellationToken cancellationToken);
}
