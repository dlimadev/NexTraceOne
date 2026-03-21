using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Abstração para resolução do EnvironmentProfile a partir de um ambiente.
/// Permite que módulos downstream determinem o comportamento correto
/// sem precisar ter acesso completo à entidade Environment.
///
/// Em fases futuras, esta interface pode ser implementada com lógica
/// de inferência baseada em nome, slug, configuração ou políticas.
/// </summary>
public interface IEnvironmentProfileResolver
{
    /// <summary>
    /// Resolve o perfil operacional de um ambiente específico de um tenant.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant proprietário do ambiente.</param>
    /// <param name="environmentId">Identificador do ambiente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O EnvironmentProfile do ambiente, ou null se não encontrado.</returns>
    Task<EnvironmentProfile?> ResolveProfileAsync(
        TenantId tenantId,
        EnvironmentId environmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se um ambiente tem comportamento similar à produção.
    /// </summary>
    Task<bool> IsProductionLikeAsync(
        TenantId tenantId,
        EnvironmentId environmentId,
        CancellationToken cancellationToken = default);
}
