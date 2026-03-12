using Microsoft.AspNetCore.Authorization;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Requisito de autorização baseado em permissão granular.
/// Cada policy vinculada a um endpoint exige que o usuário possua
/// a permissão correspondente no JWT (claim "permissions").
/// Avaliado pelo <see cref="PermissionAuthorizationHandler"/>.
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    /// <summary>Código da permissão exigida (ex.: "identity:users:write").</summary>
    public string Permission { get; } = permission;
}
