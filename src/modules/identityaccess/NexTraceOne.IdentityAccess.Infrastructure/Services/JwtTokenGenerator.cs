using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Gerador de JWT (access token) e refresh token para o módulo Identity.
/// Produz tokens HMAC-SHA256 assinados com chave simétrica configurável.
/// A configuração é lida do appsettings na seção "Jwt" (raiz) ou "Security:Jwt" (building blocks).
/// Claims incluídos: sub, email, name, tenant_id, role, role_id, permissions, nbf, exp, iss, aud.
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
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var now = dateTimeProvider.UtcNow;
        var payload = new Dictionary<string, object>
        {
            ["iss"] = _issuer,
            ["aud"] = _audience,
            ["sub"] = user.Id.Value.ToString(),
            ["email"] = user.Email.Value,
            ["name"] = user.FullName.Value,
            ["tenant_id"] = membership.TenantId.Value.ToString(),
            ["role_id"] = membership.RoleId.Value.ToString(),
            ["permissions"] = permissions,
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(_accessTokenLifetimeMinutes).ToUnixTimeSeconds()
        };

        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";
        var signature = ComputeSignature(unsignedToken, _signingKey);

        return $"{unsignedToken}.{signature}";
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeSignature(string value, string signingKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
