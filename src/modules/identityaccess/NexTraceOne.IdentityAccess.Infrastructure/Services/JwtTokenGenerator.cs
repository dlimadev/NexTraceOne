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
/// Claims incluídos: sub, email, name, tenant_id, role_id, permissions, nbf, exp, iss, aud.
/// </summary>
internal sealed class JwtTokenGenerator(IConfiguration configuration, IDateTimeProvider dateTimeProvider) : IJwtTokenGenerator
{
    private readonly string _issuer = configuration["Jwt:Issuer"]
        ?? configuration["Security:Jwt:Issuer"]
        ?? "NexTraceOne";

    private readonly string _audience = configuration["Jwt:Audience"]
        ?? configuration["Security:Jwt:Audience"]
        ?? "NexTraceOne.Clients";

    private readonly string _signingKey = configuration["Jwt:Secret"]
        ?? configuration["Security:Jwt:SigningKey"]
        ?? throw new InvalidOperationException(
            "JWT signing key is not configured. Set 'Jwt:Secret' via environment variable (Jwt__Secret), " +
            "dotnet user-secrets, or a secrets manager. " +
            "A signing key is mandatory in all environments.");

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
        var now = dateTimeProvider.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name, user.FullName.Value),
            new("tenant_id", membership.TenantId.Value.ToString()),
            new("role_id", membership.RoleId.Value.ToString()),
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permissions", permission));
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
}
