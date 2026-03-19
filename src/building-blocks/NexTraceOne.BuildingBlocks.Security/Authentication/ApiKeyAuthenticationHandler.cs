using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NexTraceOne.BuildingBlocks.Security.Authentication;

/// <summary>
/// Handler de autenticação via API key para integrações sistema-a-sistema.
///
/// Permite que sistemas externos autentiquem-se usando um header X-Api-Key
/// em vez de tokens JWT obtidos via login interativo. Cada API key está
/// associada a um tenant e conjunto de permissões configurados.
///
/// Segurança:
/// - API keys são validadas contra configuração em memória (MVP1).
/// - Cada key está vinculada a um tenantId e lista de permissões.
/// - Keys inválidas ou ausentes resultam em AuthenticateResult.NoResult()
///   para permitir fallback ao esquema JWT padrão.
/// - Em produção, migrar para armazenamento criptografado (PostgreSQL + AES-GCM).
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>Header HTTP que transporta a API key.</summary>
    private const string ApiKeyHeaderName = "X-Api-Key";

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var configuredKey = Options.ConfiguredKeys
            .FirstOrDefault(k => FixedTimeEquals(k.Key, apiKey));

        if (configuredKey is null)
        {
            Logger.LogWarning(
                "API key authentication failed: invalid key provided from {RemoteIp}",
                Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, configuredKey.ClientId),
            new(ClaimTypes.Name, configuredKey.ClientName),
            new("tenant_id", configuredKey.TenantId),
            new("auth_method", "api_key"),
        };

        foreach (var permission in configuredKey.Permissions)
        {
            claims.Add(new Claim("permissions", permission));
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogDebug(
            "API key authentication succeeded for client '{ClientId}' in tenant '{TenantId}'",
            configuredKey.ClientId,
            configuredKey.TenantId);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Comparação em tempo constante para prevenir ataques de timing.
    /// Converte ambas as strings para bytes UTF-8 e usa
    /// <see cref="CryptographicOperations.FixedTimeEquals"/> para garantir
    /// que o tempo de execução não varie com o número de caracteres corretos.
    /// </summary>
    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
