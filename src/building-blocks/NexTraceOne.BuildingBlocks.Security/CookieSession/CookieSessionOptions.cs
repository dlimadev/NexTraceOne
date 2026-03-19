namespace NexTraceOne.BuildingBlocks.Security.CookieSession;

/// <summary>
/// Opções de configuração para a sessão baseada em cookies httpOnly.
///
/// CONTEXTO DE SEGURANÇA:
/// Por padrão, o NexTraceOne usa Bearer tokens em Authorization header
/// com access token em sessionStorage e refresh token em memória.
/// Esse modelo não tem risco de CSRF porque browsers não enviam o
/// header Authorization automaticamente para outros origens.
///
/// A sessão baseada em cookie httpOnly é um modelo alternativo mais seguro
/// contra XSS (o token não é acessível por JavaScript), mas requer CSRF
/// protection porque cookies são enviados automaticamente pelo browser.
///
/// MIGRAÇÃO CONTROLADA:
/// Esta feature está DESABILITADA por padrão. Ativar apenas após:
/// 1. Validar os endpoints com testes integrados.
/// 2. Atualizar o frontend para usar os novos endpoints de sessão.
/// 3. Implementar o envio do CSRF token nas mutations do frontend.
/// 4. Validar em staging antes de produção.
///
/// Configuração: seção "Auth:CookieSession" no appsettings.json.
/// </summary>
public sealed class CookieSessionOptions
{
    public const string SectionName = "Auth:CookieSession";

    /// <summary>
    /// Ativa os endpoints de sessão via cookie httpOnly.
    /// Padrão: false — os endpoints não são registados enquanto desabilitado.
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Nome do cookie que transporta o access token.
    /// Padrão: "nxt_at".
    /// </summary>
    public string AccessTokenCookieName { get; init; } = "nxt_at";

    /// <summary>
    /// Nome do cookie não-httpOnly que transporta o CSRF token (double-submit pattern).
    /// Deve ser legível pelo JavaScript para que o SPA possa incluí-lo no header.
    /// Padrão: "nxt_csrf".
    /// </summary>
    public string CsrfCookieName { get; init; } = "nxt_csrf";

    /// <summary>
    /// Nome do header HTTP onde o SPA deve enviar o CSRF token em cada mutation.
    /// Padrão: "X-Csrf-Token".
    /// </summary>
    public string CsrfHeaderName { get; init; } = "X-Csrf-Token";

    /// <summary>
    /// Duração do cookie de access token em minutos. Deve coincidir com AccessTokenExpirationMinutes do JWT.
    /// Padrão: 60 minutos.
    /// </summary>
    public int AccessTokenCookieExpirationMinutes { get; init; } = 60;

    /// <summary>
    /// Exige cookies marcados como Secure.
    /// Deve permanecer true em produção; pode ser false apenas em development controlado.
    /// </summary>
    public bool RequireSecureCookies { get; init; } = true;
}
