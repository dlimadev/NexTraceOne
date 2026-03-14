using NexTraceOne.Identity.Contracts.DTOs;

namespace NexTraceOne.Identity.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Identity.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IIdentityModule
{
    /// <summary>Obtém um usuário pelo identificador público.</summary>
    Task<UserSummaryDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Obtém as permissões efetivas do usuário dentro de um tenant.</summary>
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Valida se o usuário possui vínculo ativo com o tenant informado.</summary>
    Task<bool> ValidateTenantMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
}
