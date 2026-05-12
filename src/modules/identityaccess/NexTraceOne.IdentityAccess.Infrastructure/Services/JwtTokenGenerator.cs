using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Gerador de JWT (access token) e refresh token para o módulo Identity.
/// Produz tokens HMAC-SHA256 assinados com chave simétrica configurável
/// usando <see cref="JwtSecurityTokenHandler"/> standard da Microsoft.
/// A configuração é lida do appsettings na seção "Jwt" (raiz) ou "Security:Jwt" (building blocks).
/// Claims incluídos: sub, email, name, tenant_id, role_ids (multi-valued), permissions, nbf, exp, iss, aud.
/// </summary>
internal sealed class JwtTokenGenerator(IConfiguration configuration, IDateTimeProvider dateTimeProvider) : IJwtTokenGenerator
{
    private readonly string _issuer = configuration["Jwt:Issuer"]
        ?? configuration["Security:Jwt:Issuer"]
        ?? "NexTraceOne";

    private readonly string _audience = configuration["Jwt:Audience"]
        ?? configuration["Security:Jwt:Audience"]
        ?? "NexTraceOne.Clients";

    private readonly string _signingKey = ValidateSigningKey(
        configuration["Jwt:Secret"]
            ?? configuration["Security:Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured. Set 'Jwt:Secret' via environment variable (Jwt__Secret), " +
                "dotnet user-secrets, or a secrets manager. " +
                "A signing key is mandatory in all environments."));

    private readonly int _accessTokenLifetimeMinutes = int.TryParse(
            configuration["Jwt:AccessTokenExpirationMinutes"]
                ?? configuration["Security:Jwt:AccessTokenLifetimeMinutes"],
            out var minutes)
        ? minutes
        : 60;

    /// <inheritdoc />
    public int AccessTokenLifetimeSeconds => _accessTokenLifetimeMinutes * 60;

    /// <inheritdoc />
    public string GenerateAccessToken(User user, TenantMembership membership, IReadOnlyCollection<string> permissions)
    {
        // Backward-compatible: sem role name disponível — usa string vazia; permissions derivadas via IClaimsTransformation.
        return GenerateAccessToken(
            user,
            membership.TenantId,
            new[] { membership.RoleId },
            Array.Empty<string>());
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user, TenantId tenantId, IReadOnlyCollection<RoleId> roleIds, IReadOnlyCollection<string> roleNames, IReadOnlyCollection<string>? capabilities = null)
    {
        var now = dateTimeProvider.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name, user.FullName.Value),
            new("tenant_id", tenantId.Value.ToString()),
        };

        // Multi-valued claim: role_ids (um claim por papel atribuído).
        foreach (var roleId in roleIds)
            claims.Add(new Claim("role_ids", roleId.Value.ToString()));

        // role_names: nomes dos papéis para derivação server-side de permissões via IClaimsTransformation.
        // Mantém o token pequeno (sem permissions[] em claro) para caber num cookie HttpOnly ≤ 4 KB.
        foreach (var roleName in roleNames)
            claims.Add(new Claim("role_names", roleName));

        // SaaS-01: capabilities claim — plano de licença do tenant.
        if (capabilities is not null)
        {
            foreach (var cap in capabilities)
                claims.Add(new Claim("capabilities", cap));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(_accessTokenLifetimeMinutes).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string ValidateSigningKey(string key)
    {
        var keyBytes = Encoding.UTF8.GetByteCount(key);
        if (keyBytes < 16)
            throw new InvalidOperationException(
                $"JWT signing key is too short ({keyBytes * 8} bits). " +
                "HS256 requires at least 128 bits (16 bytes). " +
                "Recommended minimum is 256 bits (32 bytes). " +
                "Set a longer 'Jwt:Secret' via user-secrets or environment variable.");
        return key;
    }
}
