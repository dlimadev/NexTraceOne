using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.ValueObjects;

/// <summary>
/// Value Object que agrega a configuração completa de autenticação de um tenant.
/// Combina o modo de autenticação com parâmetros operacionais como MFA, fallback local,
/// timeout de sessão e limite de sessões concorrentes.
/// <para>
/// Esta política é definida pelo administrador do tenant e aplicada de forma consistente
/// em todos os fluxos de login (local, federado, refresh token). A separação em Value Object
/// garante imutabilidade e validação centralizada das regras de negócio.
/// </para>
/// <para>
/// Regras de negócio aplicadas:
/// <list type="bullet">
///   <item>Modo Federated não permite fallback local ativo.</item>
///   <item>Modo Federated requer um provedor OIDC padrão configurado.</item>
///   <item>Timeout de sessão deve estar entre 5 e 1440 minutos (24 horas).</item>
///   <item>Máximo de sessões concorrentes deve estar entre 1 e 100.</item>
/// </list>
/// </para>
/// </summary>
public sealed class AuthenticationPolicy : ValueObject
{
    /// <summary>Timeout de sessão mínimo permitido, em minutos.</summary>
    private const int MinSessionTimeoutMinutes = 5;

    /// <summary>Timeout de sessão máximo permitido, em minutos (24 horas).</summary>
    private const int MaxSessionTimeoutMinutes = 1440;

    /// <summary>Número mínimo de sessões concorrentes por utilizador.</summary>
    private const int MinConcurrentSessions = 1;

    /// <summary>Número máximo de sessões concorrentes por utilizador.</summary>
    private const int MaxConcurrentSessions = 100;

    /// <summary>Timeout de sessão padrão, em minutos.</summary>
    private const int DefaultSessionTimeoutMinutes = 60;

    /// <summary>Número padrão de sessões concorrentes por utilizador.</summary>
    private const int DefaultMaxConcurrentSessions = 5;

    private AuthenticationPolicy(
        AuthenticationMode mode,
        bool allowLocalFallback,
        bool requireMfa,
        string? defaultOidcProvider,
        int sessionTimeoutMinutes,
        int maxConcurrentSessions)
    {
        Mode = mode;
        AllowLocalFallback = allowLocalFallback;
        RequireMfa = requireMfa;
        DefaultOidcProvider = defaultOidcProvider;
        SessionTimeoutMinutes = sessionTimeoutMinutes;
        MaxConcurrentSessionsPerUser = maxConcurrentSessions;
    }

    /// <summary>Modo primário de autenticação do tenant.</summary>
    public AuthenticationMode Mode { get; }

    /// <summary>
    /// Indica se o login local (email + senha) está disponível como fallback
    /// quando o IdP federado está indisponível. Relevante apenas para modos Hybrid e Local.
    /// </summary>
    public bool AllowLocalFallback { get; }

    /// <summary>
    /// Indica se a autenticação multifator (MFA) é obrigatória para todos os utilizadores do tenant.
    /// Em modo SaaS/Federated, MFA é tipicamente obrigatório por política de segurança.
    /// </summary>
    public bool RequireMfa { get; }

    /// <summary>
    /// Nome do provedor OIDC padrão para autenticação federada.
    /// Obrigatório quando o modo é Federated; opcional para Hybrid e Local.
    /// </summary>
    public string? DefaultOidcProvider { get; }

    /// <summary>Duração da sessão em minutos antes de expirar. Entre 5 e 1440 (24h).</summary>
    public int SessionTimeoutMinutes { get; }

    /// <summary>Número máximo de sessões simultâneas por utilizador. Entre 1 e 100.</summary>
    public int MaxConcurrentSessionsPerUser { get; }

    /// <summary>
    /// Cria uma política de autenticação validada com todos os parâmetros explícitos.
    /// Aplica regras de consistência entre modo de autenticação e parâmetros operacionais.
    /// </summary>
    /// <param name="mode">Modo primário de autenticação.</param>
    /// <param name="allowLocalFallback">Se o login local está disponível como fallback.</param>
    /// <param name="requireMfa">Se MFA é obrigatório.</param>
    /// <param name="defaultOidcProvider">Nome do provedor OIDC padrão (obrigatório para modo Federated).</param>
    /// <param name="sessionTimeoutMinutes">Duração da sessão em minutos (padrão: 60).</param>
    /// <param name="maxConcurrentSessions">Máximo de sessões concorrentes (padrão: 5).</param>
    /// <returns>Instância validada de <see cref="AuthenticationPolicy"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Quando as regras de consistência entre modo e parâmetros são violadas.
    /// </exception>
    public static AuthenticationPolicy Create(
        AuthenticationMode mode,
        bool allowLocalFallback = false,
        bool requireMfa = false,
        string? defaultOidcProvider = null,
        int sessionTimeoutMinutes = DefaultSessionTimeoutMinutes,
        int maxConcurrentSessions = DefaultMaxConcurrentSessions)
    {
        Guard.Against.Null(mode, nameof(mode));

        if (mode.IsFederated && allowLocalFallback)
        {
            throw new ArgumentException(
                "Local fallback is not allowed when authentication mode is Federated.",
                nameof(allowLocalFallback));
        }

        if (mode.IsFederated && string.IsNullOrWhiteSpace(defaultOidcProvider))
        {
            throw new ArgumentException(
                "A default OIDC provider is required when authentication mode is Federated.",
                nameof(defaultOidcProvider));
        }

        if (sessionTimeoutMinutes < MinSessionTimeoutMinutes || sessionTimeoutMinutes > MaxSessionTimeoutMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sessionTimeoutMinutes),
                sessionTimeoutMinutes,
                $"Session timeout must be between {MinSessionTimeoutMinutes} and {MaxSessionTimeoutMinutes} minutes.");
        }

        if (maxConcurrentSessions < MinConcurrentSessions || maxConcurrentSessions > MaxConcurrentSessions)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrentSessions),
                maxConcurrentSessions,
                $"Maximum concurrent sessions must be between {MinConcurrentSessions} and {MaxConcurrentSessions}.");
        }

        var normalizedProvider = defaultOidcProvider?.Trim();

        return new AuthenticationPolicy(
            mode,
            allowLocalFallback,
            requireMfa,
            normalizedProvider,
            sessionTimeoutMinutes,
            maxConcurrentSessions);
    }

    /// <summary>
    /// Cria a política de autenticação padrão para tenants SaaS.
    /// Modo federado obrigatório, MFA obrigatório, sem fallback local.
    /// Sessão de 30 minutos e máximo de 3 sessões concorrentes para maior segurança.
    /// </summary>
    /// <param name="oidcProvider">Nome do provedor OIDC corporativo (ex: "AzureAD", "Okta").</param>
    /// <returns>Política configurada para ambiente SaaS.</returns>
    public static AuthenticationPolicy ForSaaS(string oidcProvider)
    {
        Guard.Against.NullOrWhiteSpace(oidcProvider, nameof(oidcProvider));

        return Create(
            mode: AuthenticationMode.Federated,
            allowLocalFallback: false,
            requireMfa: true,
            defaultOidcProvider: oidcProvider,
            sessionTimeoutMinutes: 30,
            maxConcurrentSessions: 3);
    }

    /// <summary>
    /// Cria a política de autenticação padrão para instalações self-hosted.
    /// Modo híbrido com fallback local habilitado, MFA opcional.
    /// Sessão de 60 minutos e até 5 sessões concorrentes.
    /// </summary>
    /// <returns>Política configurada para ambiente self-hosted.</returns>
    public static AuthenticationPolicy ForSelfHosted()
    {
        return Create(
            mode: AuthenticationMode.Hybrid,
            allowLocalFallback: true,
            requireMfa: false,
            defaultOidcProvider: null,
            sessionTimeoutMinutes: DefaultSessionTimeoutMinutes,
            maxConcurrentSessions: DefaultMaxConcurrentSessions);
    }

    /// <summary>
    /// Cria a política de autenticação padrão com valores sensatos.
    /// Modo híbrido, fallback local habilitado, MFA desativado.
    /// Útil para configuração inicial antes de o administrador personalizar a política.
    /// </summary>
    /// <returns>Política padrão com configuração equilibrada.</returns>
    public static AuthenticationPolicy Default()
    {
        return Create(
            mode: AuthenticationMode.Hybrid,
            allowLocalFallback: true,
            requireMfa: false,
            defaultOidcProvider: null,
            sessionTimeoutMinutes: DefaultSessionTimeoutMinutes,
            maxConcurrentSessions: DefaultMaxConcurrentSessions);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Mode;
        yield return AllowLocalFallback;
        yield return RequireMfa;
        yield return DefaultOidcProvider;
        yield return SessionTimeoutMinutes;
        yield return MaxConcurrentSessionsPerUser;
    }

    /// <summary>Retorna uma representação textual resumida da política.</summary>
    public override string ToString()
        => $"AuthenticationPolicy(Mode={Mode}, MFA={RequireMfa}, Timeout={SessionTimeoutMinutes}min)";
}
