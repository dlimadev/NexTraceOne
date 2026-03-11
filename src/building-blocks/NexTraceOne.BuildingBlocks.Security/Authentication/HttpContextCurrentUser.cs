using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Security.Authentication;

/// <summary>
/// Implementação de usuário atual baseada no HttpContext.
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string Id
        => User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? string.Empty;

    /// <inheritdoc />
    public string Name
        => User?.FindFirstValue(ClaimTypes.Name)
            ?? User?.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? string.Empty;

    /// <inheritdoc />
    public string Email
        => User?.FindFirstValue(ClaimTypes.Email)
            ?? User?.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? string.Empty;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public bool HasPermission(string permission)
        => User?.FindAll("permissions").Any(claim => string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase)) == true;
}
