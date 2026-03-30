using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Provider dinâmico de policies baseadas em permissão e módulo/página/ação.
///
/// Suporta dois prefixos:
/// - <c>"Permission:"</c> → cria <see cref="PermissionRequirement"/> (modelo plano legacy).
/// - <c>"ModuleAccess:"</c> → cria <see cref="ModuleAccessRequirement"/> (modelo granular novo).
///
/// Quando um endpoint exige uma policy cujo nome começa com "Permission:",
/// este provider cria automaticamente a policy com o PermissionRequirement correspondente.
///
/// Quando um endpoint exige uma policy cujo nome começa com "ModuleAccess:",
/// no formato "ModuleAccess:Module:Page:Action", este provider cria a policy
/// com o ModuleAccessRequirement correspondente.
///
/// Evita registrar manualmente cada permissão como policy estática.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    /// <summary>Prefixo que identifica policies de permissão plana gerenciadas por este provider.</summary>
    internal const string PolicyPrefix = "Permission:";

    /// <summary>Prefixo que identifica policies de acesso módulo/página/ação gerenciadas por este provider.</summary>
    internal const string ModuleAccessPrefix = "ModuleAccess:";

    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider = new(options);

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // 1. Policy de permissão plana: "Permission:ai:runtime:write"
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PolicyPrefix.Length..];

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // 2. Policy de acesso módulo/página/ação: "ModuleAccess:AI:Runtime:Write"
        if (policyName.StartsWith(ModuleAccessPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var parts = policyName[ModuleAccessPrefix.Length..].Split(':');
            if (parts.Length == 3)
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new ModuleAccessRequirement(parts[0], parts[1], parts[2]))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        return _fallbackProvider.GetPolicyAsync(policyName);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallbackProvider.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallbackProvider.GetFallbackPolicyAsync();
}
