using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.ValueObjects;

/// <summary>
/// Política de gestão de sessões aplicável a um tenant ou contexto.
/// Define limites de sessões concorrentes, timeouts e regras de re-autenticação.
/// <para>
/// Complementa <see cref="AuthenticationPolicy"/> e <see cref="MfaPolicy"/> ao definir
/// os parâmetros operacionais de sessão de forma independente, permitindo configurações
/// diferenciadas por deployment model (SaaS, self-hosted, on-premise).
/// </para>
/// <para>
/// Regras de negócio aplicadas:
/// <list type="bullet">
///   <item>Sessões concorrentes devem estar entre 1 e 100.</item>
///   <item>Timeout de sessão deve estar entre 5 e 1440 minutos (24 horas).</item>
///   <item>Timeout de inatividade deve estar entre 1 e o timeout de sessão.</item>
///   <item>RememberMe em dias deve estar entre 1 e 90.</item>
///   <item>Se AllowRememberMe é falso, RememberMeDays é ignorado.</item>
/// </list>
/// </para>
/// </summary>
public sealed class SessionPolicy : ValueObject
{
    /// <summary>Número mínimo de sessões concorrentes por utilizador.</summary>
    private const int MinConcurrentSessions = 1;

    /// <summary>Número máximo de sessões concorrentes por utilizador.</summary>
    private const int MaxConcurrentSessionsLimit = 100;

    /// <summary>Timeout de sessão mínimo, em minutos.</summary>
    private const int MinSessionTimeoutMinutes = 5;

    /// <summary>Timeout de sessão máximo, em minutos (24 horas).</summary>
    private const int MaxSessionTimeoutMinutes = 1440;

    /// <summary>Timeout de inatividade mínimo, em minutos.</summary>
    private const int MinIdleTimeoutMinutes = 1;

    /// <summary>Período mínimo de RememberMe, em dias.</summary>
    private const int MinRememberMeDays = 1;

    /// <summary>Período máximo de RememberMe, em dias.</summary>
    private const int MaxRememberMeDays = 90;

    private SessionPolicy(
        int maxConcurrentSessions,
        int sessionTimeoutMinutes,
        int idleTimeoutMinutes,
        bool requireReauthForSensitiveOps,
        bool allowRememberMe,
        int rememberMeDays)
    {
        MaxConcurrentSessions = maxConcurrentSessions;
        SessionTimeoutMinutes = sessionTimeoutMinutes;
        IdleTimeoutMinutes = idleTimeoutMinutes;
        RequireReauthForSensitiveOps = requireReauthForSensitiveOps;
        AllowRememberMe = allowRememberMe;
        RememberMeDays = rememberMeDays;
    }

    /// <summary>Número máximo de sessões simultâneas por utilizador.</summary>
    public int MaxConcurrentSessions { get; }

    /// <summary>Duração total da sessão em minutos antes de expirar.</summary>
    public int SessionTimeoutMinutes { get; }

    /// <summary>Tempo de inatividade em minutos antes de expirar a sessão.</summary>
    public int IdleTimeoutMinutes { get; }

    /// <summary>
    /// Indica se re-autenticação é exigida para operações sensíveis
    /// (ex: alteração de email, remoção de sessões, exportação de dados).
    /// </summary>
    public bool RequireReauthForSensitiveOps { get; }

    /// <summary>Indica se a opção "Lembrar-me" está disponível no login.</summary>
    public bool AllowRememberMe { get; }

    /// <summary>
    /// Número de dias que a sessão persiste quando "Lembrar-me" está ativo.
    /// Relevante apenas quando <see cref="AllowRememberMe"/> é verdadeiro.
    /// </summary>
    public int RememberMeDays { get; }

    /// <summary>
    /// Cria uma política de sessão validada com todos os parâmetros explícitos.
    /// Aplica regras de consistência entre os parâmetros de sessão.
    /// </summary>
    /// <param name="maxConcurrentSessions">Máximo de sessões concorrentes (1–100).</param>
    /// <param name="sessionTimeoutMinutes">Timeout total da sessão em minutos (5–1440).</param>
    /// <param name="idleTimeoutMinutes">Timeout de inatividade em minutos (1–sessionTimeout).</param>
    /// <param name="requireReauthForSensitiveOps">Se re-autenticação é exigida para operações sensíveis.</param>
    /// <param name="allowRememberMe">Se a opção "Lembrar-me" é permitida.</param>
    /// <param name="rememberMeDays">Dias de persistência do "Lembrar-me" (1–90).</param>
    /// <returns>Instância validada de <see cref="SessionPolicy"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Quando os parâmetros numéricos estão fora dos limites permitidos.
    /// </exception>
    public static SessionPolicy Create(
        int maxConcurrentSessions = 5,
        int sessionTimeoutMinutes = 60,
        int idleTimeoutMinutes = 30,
        bool requireReauthForSensitiveOps = true,
        bool allowRememberMe = false,
        int rememberMeDays = 30)
    {
        if (maxConcurrentSessions < MinConcurrentSessions || maxConcurrentSessions > MaxConcurrentSessionsLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrentSessions),
                maxConcurrentSessions,
                $"Max concurrent sessions must be between {MinConcurrentSessions} and {MaxConcurrentSessionsLimit}.");
        }

        if (sessionTimeoutMinutes < MinSessionTimeoutMinutes || sessionTimeoutMinutes > MaxSessionTimeoutMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sessionTimeoutMinutes),
                sessionTimeoutMinutes,
                $"Session timeout must be between {MinSessionTimeoutMinutes} and {MaxSessionTimeoutMinutes} minutes.");
        }

        if (idleTimeoutMinutes < MinIdleTimeoutMinutes || idleTimeoutMinutes > sessionTimeoutMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idleTimeoutMinutes),
                idleTimeoutMinutes,
                $"Idle timeout must be between {MinIdleTimeoutMinutes} and {sessionTimeoutMinutes} minutes (session timeout).");
        }

        if (allowRememberMe && (rememberMeDays < MinRememberMeDays || rememberMeDays > MaxRememberMeDays))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rememberMeDays),
                rememberMeDays,
                $"RememberMe days must be between {MinRememberMeDays} and {MaxRememberMeDays}.");
        }

        var effectiveRememberMeDays = allowRememberMe ? rememberMeDays : 0;

        return new SessionPolicy(
            maxConcurrentSessions,
            sessionTimeoutMinutes,
            idleTimeoutMinutes,
            requireReauthForSensitiveOps,
            allowRememberMe,
            effectiveRememberMeDays);
    }

    /// <summary>
    /// Política de sessão para ambientes SaaS: restritiva, sem "Lembrar-me".
    /// Sessão de 30 minutos, inatividade de 15 minutos, máximo 3 sessões,
    /// re-autenticação obrigatória para operações sensíveis.
    /// </summary>
    public static SessionPolicy ForSaaS()
        => new(3, 30, 15, true, false, 0);

    /// <summary>
    /// Política de sessão para self-hosted: equilibrada, "Lembrar-me" por 14 dias.
    /// Sessão de 60 minutos, inatividade de 30 minutos, máximo 5 sessões.
    /// </summary>
    public static SessionPolicy ForSelfHosted()
        => new(5, 60, 30, true, true, 14);

    /// <summary>
    /// Política de sessão para on-premise: flexível, "Lembrar-me" por 30 dias.
    /// Sessão de 120 minutos, inatividade de 60 minutos, máximo 10 sessões.
    /// </summary>
    public static SessionPolicy ForOnPremise()
        => new(10, 120, 60, false, true, 30);

    /// <summary>
    /// Política de sessão padrão com valores equilibrados.
    /// Sessão de 60 minutos, inatividade de 30 minutos, máximo 5 sessões.
    /// </summary>
    public static SessionPolicy Default()
        => new(5, 60, 30, true, false, 0);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxConcurrentSessions;
        yield return SessionTimeoutMinutes;
        yield return IdleTimeoutMinutes;
        yield return RequireReauthForSensitiveOps;
        yield return AllowRememberMe;
        yield return RememberMeDays;
    }

    /// <summary>Retorna uma representação textual resumida da política de sessão.</summary>
    public override string ToString()
        => $"SessionPolicy(MaxSessions={MaxConcurrentSessions}, Timeout={SessionTimeoutMinutes}min, Idle={IdleTimeoutMinutes}min, RememberMe={AllowRememberMe})";
}
