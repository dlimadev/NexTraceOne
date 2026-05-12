using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Enriquece o ClaimsPrincipal com claims "permissions" derivados dos "role_names" presentes no JWT.
/// Executado automaticamente pelo pipeline de autenticação do ASP.NET Core em cada requisição.
/// Evita que as permissões sejam armazenadas no token (que ficaria demasiado grande para cookie).
/// </summary>
internal sealed class RolePermissionsClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Idempotência: se já existirem claims de permissão, não adicionar novamente.
        if (principal.HasClaim(c => c.Type == "permissions"))
            return Task.FromResult(principal);

        var roleNames = principal.FindAll("role_names").Select(c => c.Value).ToList();
        if (roleNames.Count == 0)
            return Task.FromResult(principal);

        var permissions = roleNames
            .SelectMany(name => RolePermissionCatalog.GetPermissionsForRole(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (permissions.Count == 0)
            return Task.FromResult(principal);

        var identity = new ClaimsIdentity();
        foreach (var permission in permissions)
            identity.AddClaim(new Claim("permissions", permission));

        principal.AddIdentity(identity);
        return Task.FromResult(principal);
    }
}
