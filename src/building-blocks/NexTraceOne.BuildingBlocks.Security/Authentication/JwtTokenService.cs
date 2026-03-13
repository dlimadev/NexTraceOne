using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NexTraceOne.BuildingBlocks.Security.Authentication;

/// <summary>
/// Serviço de geração e validação de JWT tokens.
/// Suporta: access token (curta duração), refresh token (longa duração).
/// Claims incluídos: sub, email, name, tenant_id, permissions.
/// </summary>
public sealed class JwtTokenService(
    IConfiguration configuration,
    IDateTimeProvider dateTimeProvider)
{
    private readonly string _issuer = configuration["Security:Jwt:Issuer"] ?? "NexTraceOne";
    private readonly string _audience = configuration["Security:Jwt:Audience"] ?? "NexTraceOne.Clients";
    // Segurança: a chave JWT DEVE ser configurada externamente em produção.
    // Em Development, permite fallback para chave conhecida — apenas para conveniência local.
    // A ausência da chave em ambientes não-Development impede a inicialização, evitando
    // que tokens possam ser forjados usando uma chave publicamente conhecida.
    private readonly string _signingKey = ResolveSigningKey(configuration);

    private static string ResolveSigningKey(IConfiguration configuration)
    {
        var key = configuration["Security:Jwt:SigningKey"]
            ?? configuration["Jwt:Secret"];

        if (!string.IsNullOrWhiteSpace(key))
            return key;

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured. Set 'Security:Jwt:SigningKey' or 'Jwt:Secret'. " +
                "A signing key is mandatory in non-development environments.");
        }

        return "development-signing-key-development-signing-key-1234567890";
    }
    private readonly int _accessTokenLifetimeMinutes = int.TryParse(configuration["Security:Jwt:AccessTokenLifetimeMinutes"], out var minutes)
        ? minutes
        : 60;

    /// <summary>Gera um JWT de acesso contendo claims do usuário e do tenant.</summary>
    public string GenerateAccessToken(
        string subject,
        string email,
        string name,
        Guid tenantId,
        IReadOnlyCollection<string> permissions)
    {
        var now = dateTimeProvider.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, name),
            new("tenant_id", tenantId.ToString())
        };

        claims.AddRange(permissions.Select(permission => new Claim("permissions", permission)));

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

    /// <summary>Gera um refresh token criptograficamente seguro.</summary>
    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <summary>Valida um token JWT e retorna o principal autenticado.</summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var handler = new JwtSecurityTokenHandler();

        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}
