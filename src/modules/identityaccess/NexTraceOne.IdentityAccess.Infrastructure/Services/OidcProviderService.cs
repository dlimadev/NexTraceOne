using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação de referência do IOidcProvider para MVP1.
///
/// Esta implementação suporta o fluxo Authorization Code padrão OIDC para
/// provedores configurados via appsettings (OidcProviders section).
///
/// Configuração esperada no appsettings.json:
/// <code>
/// "OidcProviders": {
///   "azure": {
///     "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
///     "ClientId": "your-client-id",
///     "ClientSecret": "your-client-secret",
///     "Scopes": "openid profile email"
///   },
///   "google": {
///     "Authority": "https://accounts.google.com",
///     "ClientId": "your-client-id",
///     "ClientSecret": "your-client-secret",
///     "Scopes": "openid profile email"
///   }
/// }
/// </code>
///
/// Para MVP1, a discovery do endpoint de autorização usa o padrão OIDC discovery:
/// {authority}/.well-known/openid-configuration
///
/// NOTA: Para produção, implementar cache de discovery documents e rotação de tokens.
/// </summary>
internal sealed class OidcProviderService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<OidcProviderService> logger) : IOidcProvider
{
    /// <summary>
    /// Verifica se um provider está configurado verificando a seção de configuração.
    /// Retorna false se o provider não estiver na configuração ou estiver incompleto.
    /// </summary>
    public bool IsConfigured(string provider)
    {
        var section = configuration.GetSection($"OidcProviders:{provider.ToLowerInvariant()}");
        return section.Exists()
            && !string.IsNullOrWhiteSpace(section["ClientId"])
            && !string.IsNullOrWhiteSpace(section["Authority"]);
    }

    /// <summary>
    /// Constrói a URL de autorização do provider OIDC.
    /// Usa o endpoint de autorização descoberto via OIDC discovery ou configurado manualmente.
    /// </summary>
    public string BuildAuthorizationUrl(string provider, string state, string redirectUri)
    {
        var section = configuration.GetSection($"OidcProviders:{provider.ToLowerInvariant()}");
        var authority = section["Authority"] ?? throw new InvalidOperationException($"OIDC authority not configured for provider '{provider}'.");
        var clientId = section["ClientId"] ?? throw new InvalidOperationException($"OIDC clientId not configured for provider '{provider}'.");
        var scopes = section["Scopes"] ?? "openid profile email";

        // Constrói URL de autorização padrão OIDC Authorization Code flow
        // Para produção: usar discovery document para resolver o authorization_endpoint
        var authorizationEndpoint = $"{authority.TrimEnd('/')}/authorize";

        var queryParams = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = scopes,
            ["state"] = state
        };

        var queryString = string.Join("&", queryParams.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{authorizationEndpoint}?{queryString}";
    }

    /// <summary>
    /// Troca o authorization code por informações do usuário.
    ///
    /// Processo:
    /// 1. Chama o token endpoint do provider com o code.
    /// 2. Obtém access_token e id_token.
    /// 3. Delega a decodificação do id_token para <see cref="IdTokenDecoder"/>.
    /// 4. Retorna OidcUserInfo com sub, email e name.
    ///
    /// A separação entre comunicação HTTP (esta classe) e decodificação JWT
    /// (IdTokenDecoder) permite testar cada responsabilidade isoladamente.
    /// </summary>
    public async Task<OidcUserInfo> ExchangeCodeAsync(
        string provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var section = configuration.GetSection($"OidcProviders:{provider.ToLowerInvariant()}");
        var authority = section["Authority"] ?? throw new InvalidOperationException($"OIDC authority not configured for provider '{provider}'.");
        var clientId = section["ClientId"] ?? throw new InvalidOperationException($"OIDC clientId not configured for provider '{provider}'.");
        var clientSecret = section["ClientSecret"] ?? throw new InvalidOperationException($"OIDC clientSecret not configured for provider '{provider}'.");

        var idToken = await ExchangeCodeForIdTokenAsync(
            provider, code, redirectUri, authority, clientId, clientSecret, cancellationToken);

        return ExtractUserInfoFromIdToken(idToken, provider);
    }

    /// <summary>
    /// Executa a chamada HTTP ao token endpoint do provider OIDC e retorna o id_token bruto.
    /// Responsabilidade isolada: comunicação com o provider — sem parsing de JWT.
    /// </summary>
    private async Task<string> ExchangeCodeForIdTokenAsync(
        string provider,
        string code,
        string redirectUri,
        string authority,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        // Token endpoint padrão — para produção, descobrir via discovery document
        var tokenEndpoint = $"{authority.TrimEnd('/')}/token";

        var httpClient = httpClientFactory.CreateClient("oidc");

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(tokenRequest)
        };

        HttpResponseMessage tokenResponse;
        try
        {
            tokenResponse = await httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to reach OIDC token endpoint for provider '{Provider}'.", provider);
            throw new InvalidOperationException($"Failed to reach OIDC token endpoint for provider '{provider}'.", ex);
        }

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "OIDC token exchange failed for provider '{Provider}'. Status: {Status}. Body: {Body}",
                provider,
                tokenResponse.StatusCode,
                errorBody);
            throw new InvalidOperationException($"OIDC token exchange failed for provider '{provider}'. Status: {tokenResponse.StatusCode}");
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException($"Empty token response from provider '{provider}'.");

        var idToken = tokenJson.RootElement.TryGetProperty("id_token", out var idTokenElement)
            ? idTokenElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new InvalidOperationException($"No id_token returned by provider '{provider}'.");
        }

        return idToken;
    }

    /// <summary>
    /// Extrai informações do usuário a partir do id_token JWT decodificado.
    /// Delega a decodificação de claims para <see cref="IdTokenDecoder"/> (SRP).
    /// Valida que as claims obrigatórias (sub, email) estão presentes.
    /// </summary>
    private static OidcUserInfo ExtractUserInfoFromIdToken(string idToken, string provider)
    {
        var claims = IdTokenDecoder.DecodeClaims(idToken);

        var sub = claims.TryGetValue("sub", out var subVal) ? subVal : null;
        var email = claims.TryGetValue("email", out var emailVal) ? emailVal : null;
        var name = claims.TryGetValue("name", out var nameVal) ? nameVal
            : (claims.TryGetValue("preferred_username", out var prefUsernameVal) ? prefUsernameVal : email);

        if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException($"Id token from provider '{provider}' is missing required claims (sub, email).");
        }

        return new OidcUserInfo(sub, email, name ?? email, provider);
    }
}
