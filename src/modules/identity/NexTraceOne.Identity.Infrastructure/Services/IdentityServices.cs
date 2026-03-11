using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Contracts.DTOs;
using NexTraceOne.Identity.Contracts.ServiceInterfaces;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NexTraceOne.Identity.Infrastructure.Services;

internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => HashedPassword.FromPlainText(password).Value;

    public bool Verify(string password, string hash) => HashedPassword.FromHash(hash).Verify(password);
}

internal sealed class JwtTokenGenerator(IConfiguration configuration, IDateTimeProvider dateTimeProvider) : IJwtTokenGenerator
{
    private readonly string _issuer = configuration["Identity:Jwt:Issuer"] ?? "NexTraceOne";
    private readonly string _audience = configuration["Identity:Jwt:Audience"] ?? "NexTraceOne.Clients";
    private readonly string _signingKey = configuration["Identity:Jwt:SigningKey"] ?? "development-signing-key-development-signing-key-1234567890";
    private readonly int _accessTokenLifetimeMinutes = int.TryParse(configuration["Identity:Jwt:AccessTokenLifetimeMinutes"], out var minutes)
        ? minutes
        : 60;

    public int AccessTokenLifetimeSeconds => _accessTokenLifetimeMinutes * 60;

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

internal sealed class IdentityModuleService(
    IUserRepository userRepository,
    ITenantMembershipRepository membershipRepository,
    IRoleRepository roleRepository) : IIdentityModule
{
    public async Task<UserSummaryDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(UserId.From(userId), cancellationToken);
        return user is null
            ? null
            : new UserSummaryDto(user.Id.Value, user.Email.Value, user.FullName.Value, user.IsActive);
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByUserAndTenantAsync(
            UserId.From(userId),
            TenantId.From(tenantId),
            cancellationToken);

        if (membership is null || !membership.IsActive)
        {
            return [];
        }

        var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
        return role is null ? [] : Role.GetPermissionsForRole(role.Name);
    }

    public async Task<bool> ValidateTenantMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByUserAndTenantAsync(
            UserId.From(userId),
            TenantId.From(tenantId),
            cancellationToken);

        return membership is { IsActive: true };
    }
}
