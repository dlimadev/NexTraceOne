using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Abstractions;

/// <summary>
/// Abstração para integração com provedores de identidade OIDC externos.
///
/// Permite suportar múltiplos providers (Azure AD, Google, Okta, Keycloak, etc.)
/// com configuração por tenant. A implementação concreta em Infrastructure
/// usa ASP.NET Core OIDC middleware ou chamadas diretas à API do provider.
///
/// Fluxo OIDC Authorization Code (PKCE recomendado):
/// 1. StartAsync → gera URL de redirect + state para validação CSRF
/// 2. Provider redireciona para callback com code + state
/// 3. ExchangeCodeAsync → troca code por tokens
/// 4. GetUserInfoAsync → obtém dados do usuário com access_token
///
/// Segurança:
/// - State deve ser validado no callback para prevenir CSRF.
/// - PKCE (code_verifier/code_challenge) deve ser usado quando suportado.
/// - Tokens recebidos do provider nunca são armazenados — apenas o JWT interno.
/// </summary>
public interface IOidcProvider
{
    /// <summary>
    /// Verifica se um provider OIDC está configurado para este tenant.
    /// Retorna false se o provider não foi configurado pelo admin.
    /// </summary>
    bool IsConfigured(string provider);

    /// <summary>
    /// Gera a URL de redirect para o provider OIDC com state seguro.
    /// O state é um nonce opaco que deve ser validado no callback para prevenir CSRF.
    /// </summary>
    /// <param name="provider">Nome do provider (e.g., "azure", "google", "okta").</param>
    /// <param name="state">Nonce gerado pelo backend para validação CSRF.</param>
    /// <param name="redirectUri">URI de callback registrado no provider.</param>
    /// <returns>URL completa para redirect do browser ao provider.</returns>
    string BuildAuthorizationUrl(string provider, string state, string redirectUri);

    /// <summary>
    /// Troca o authorization code recebido no callback por informações do usuário.
    /// O code é de uso único e tem TTL curto (geralmente 5 minutos).
    /// </summary>
    /// <param name="provider">Nome do provider que emitiu o code.</param>
    /// <param name="code">Authorization code recebido no callback.</param>
    /// <param name="redirectUri">URI de callback registrado (deve ser idêntico ao usado em BuildAuthorizationUrl).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Informações do usuário autenticado no provider.</returns>
    Task<OidcUserInfo> ExchangeCodeAsync(
        string provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken);
}

/// <summary>Informações do usuário retornadas pelo provider OIDC após troca do code.</summary>
public sealed record OidcUserInfo(
    /// <summary>Identificador único do usuário no provider (sub claim).</summary>
    string ExternalId,

    /// <summary>Email do usuário no provider.</summary>
    string Email,

    /// <summary>Nome de exibição do usuário.</summary>
    string DisplayName,

    /// <summary>Nome do provider que autenticou (e.g., "azure", "google").</summary>
    string Provider);
