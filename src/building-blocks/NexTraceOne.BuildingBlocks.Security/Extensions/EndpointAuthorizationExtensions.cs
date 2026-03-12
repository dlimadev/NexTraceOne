using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Security.Authorization;

namespace NexTraceOne.BuildingBlocks.Security.Extensions;

/// <summary>
/// Extensões para aplicar autorização baseada em permissão a endpoints Minimal API.
///
/// Uso: endpoint.RequirePermission("identity:users:write")
/// Resolve automaticamente a policy via <see cref="PermissionPolicyProvider"/>.
/// </summary>
public static class EndpointAuthorizationExtensions
{
    /// <summary>
    /// Exige que o usuário possua a permissão especificada para acessar o endpoint.
    /// Combina autenticação obrigatória com verificação granular de permissão.
    /// </summary>
    /// <param name="builder">Builder do endpoint Minimal API.</param>
    /// <param name="permission">Código da permissão exigida (ex.: "identity:users:write").</param>
    /// <returns>O builder com a policy de autorização aplicada.</returns>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization($"{PermissionPolicyProvider.PolicyPrefix}{permission}");
    }
}
