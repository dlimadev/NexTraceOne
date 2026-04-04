using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Configuration;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação de tokens de desafio MFA de curta duração assinados com HMAC-SHA256.
///
/// Formato do token: Base64Url("{userId}:{expiryUnixSeconds}:{hmac}")
/// onde hmac = HMACSHA256("{userId}:{expiryUnixSeconds}", signingKey).
///
/// Design:
/// - Stateless: não requer tabela de base de dados adicional.
/// - Prazo curto (padrão: 5 minutos) para limitar janela de ataque.
/// - Assinatura HMAC impede adulteração do UserId ou do prazo.
/// - Reutiliza a chave JWT da plataforma para assinar o token.
/// </summary>
internal sealed class MfaChallengeTokenService(
    IConfiguration configuration,
    IDateTimeProvider dateTimeProvider) : IMfaChallengeTokenService
{
    private readonly string _signingKey = configuration["Jwt:Secret"]
        ?? configuration["Security:Jwt:SigningKey"]
        ?? throw new InvalidOperationException("JWT signing key is not configured.");

    /// <inheritdoc />
    public string Issue(Guid userId, DateTimeOffset expiresAt)
    {
        var expiry = expiresAt.ToUnixTimeSeconds();
        var payload = $"{userId}:{expiry}";
        var hmac = ComputeHmac(payload);
        var token = $"{payload}:{hmac}";
        return Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    /// <inheritdoc />
    public bool TryValidate(string token, out Guid userId)
    {
        userId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Base64UrlDecode(token));
            var parts = decoded.Split(':');
            if (parts.Length != 3)
                return false;

            if (!Guid.TryParse(parts[0], out var parsedUserId))
                return false;

            if (!long.TryParse(parts[1], out var expirySeconds))
                return false;

            var expiry = DateTimeOffset.FromUnixTimeSeconds(expirySeconds);
            if (expiry < dateTimeProvider.UtcNow)
                return false;

            var expectedHmac = ComputeHmac($"{parts[0]}:{parts[1]}");
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedHmac),
                    Encoding.UTF8.GetBytes(parts[2])))
                return false;

            userId = parsedUserId;
            return true;
        }
        catch
        {
            System.Diagnostics.Trace.TraceWarning("MfaChallengeTokenService: Failed to validate MFA challenge token.");
            return false;
        }
    }

    private string ComputeHmac(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signingKey + ":mfa-challenge"));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Base64UrlEncode(byte[] input)
        => Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        var padding = base64.Length % 4;
        if (padding > 0)
            base64 += new string('=', 4 - padding);
        return Convert.FromBase64String(base64);
    }
}
