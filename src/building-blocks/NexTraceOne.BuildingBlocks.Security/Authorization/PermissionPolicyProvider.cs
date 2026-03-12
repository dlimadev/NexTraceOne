using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Provider dinâmico de policies baseadas em permissão.
///
/// Quando um endpoint exige uma policy cujo nome começa com "Permission:",
/// este provider cria automaticamente a policy com o <see cref="PermissionRequirement"/>
/// correspondente. Evita registrar manualmente cada uma das 30+ permissões.
///
/// Exemplo: RequireAuthorization("Permission:identity:users:write")
/// → cria policy que exige a permissão "identity:users:write".
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    /// <summary>Prefixo que identifica policies de permissão gerenciadas por este provider.</summary>
    internal const string PolicyPrefix = "Permission:";

    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider = new(options);

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return _fallbackProvider.GetPolicyAsync(policyName);
        }

        var permission = policyName[PolicyPrefix.Length..];

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallbackProvider.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallbackProvider.GetFallbackPolicyAsync();
}
