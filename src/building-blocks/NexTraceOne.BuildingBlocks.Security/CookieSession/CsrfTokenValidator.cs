using Microsoft.AspNetCore.Http;

namespace NexTraceOne.BuildingBlocks.Security.CookieSession;

/// <summary>
/// Valida tokens CSRF usando o padrão double-submit cookie.
///
/// PADRÃO DOUBLE-SUBMIT COOKIE:
/// O servidor emite um token CSRF aleatório e o coloca em dois lugares:
/// 1. Cookie não-httpOnly (legível pelo JavaScript do mesmo origin).
/// 2. Retorna no body da resposta de login para o SPA armazenar em memória.
///
/// Em cada mutation (POST/PUT/DELETE/PATCH), o SPA envia o token CSRF
/// como header X-Csrf-Token. O servidor valida que o header coincide
/// com o valor do cookie.
///
/// SEGURANÇA:
/// - Um atacante de outra origem não consegue ler o cookie (Same-Origin Policy).
/// - Logo, não consegue forjar o header X-Csrf-Token com o valor correto.
/// - Combinado com SameSite=Strict no cookie de auth, a proteção é dupla.
/// - Se XSS na mesma origem ocorrer, ambos os tokens ficam expostos —
///   mas isso é o modelo de ameaça do XSS, não do CSRF.
///
/// Para sessão sem cookie (Bearer token em header), CSRF NÃO é necessário.
/// Browsers não enviam Authorization headers automaticamente para outros origins.
/// </summary>
public static class CsrfTokenValidator
{
    /// <summary>
    /// Gera um CSRF token aleatório seguro (32 bytes em Base64 URL-safe).
    /// </summary>
    public static string Generate()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Valida se o token no header coincide com o token no cookie.
    /// Retorna false se a sessão cookie não estiver ativa (cookie de auth ausente).
    /// Ignora a validação para requests de método GET/HEAD/OPTIONS (safe methods).
    /// </summary>
    public static bool IsValid(
        HttpContext context,
        CookieSessionOptions options)
    {
        // Safe methods (GET, HEAD, OPTIONS) não modificam estado — não precisam de CSRF
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            return true;
        }

        // Se não há cookie de auth, a sessão cookie não está ativa — ignora CSRF
        var authCookie = context.Request.Cookies[options.AccessTokenCookieName];
        if (string.IsNullOrEmpty(authCookie))
        {
            return true;
        }

        // Se a sessão cookie está ativa, valida CSRF
        var csrfHeader = context.Request.Headers[options.CsrfHeaderName].FirstOrDefault();
        var csrfCookie = context.Request.Cookies[options.CsrfCookieName];

        if (string.IsNullOrEmpty(csrfHeader) || string.IsNullOrEmpty(csrfCookie))
        {
            return false;
        }

        // Comparação constant-time para prevenir timing attacks
        return CryptographicEquals(csrfHeader, csrfCookie);
    }

    /// <summary>
    /// Aplica os cookies de sessão httpOnly + CSRF no HttpResponse.
    /// Chamado após autenticação bem-sucedida.
    /// </summary>
    public static string ApplyCookies(
        HttpResponse response,
        string accessToken,
        CookieSessionOptions options)
    {
        var csrfToken = Generate();
        var expiry = DateTimeOffset.UtcNow.AddMinutes(options.AccessTokenCookieExpirationMinutes);

        // Cookie httpOnly — não acessível por JavaScript
        response.Cookies.Append(options.AccessTokenCookieName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiry,
            Path = "/",
        });

        // Cookie CSRF — não-httpOnly, legível pelo SPA do mesmo origin
        response.Cookies.Append(options.CsrfCookieName, csrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiry,
            Path = "/",
        });

        return csrfToken;
    }

    /// <summary>
    /// Remove os cookies de sessão (logout).
    /// </summary>
    public static void ClearCookies(HttpResponse response, CookieSessionOptions options)
    {
        response.Cookies.Delete(options.AccessTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });

        response.Cookies.Delete(options.CsrfCookieName, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
    }

    private static bool CryptographicEquals(string a, string b)
    {
        var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
        var bBytes = System.Text.Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
