using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.ValueObjects;

/// <summary>
/// Política de Multi-Factor Authentication (MFA) aplicável a um tenant ou contexto.
/// Define quando e como MFA é exigido, incluindo step-up para operações críticas.
/// <para>
/// Preparação para enforcement real de MFA em ações como vendor ops,
/// break glass, delegação e alterações de licenciamento.
/// </para>
/// <para>
/// Regras de negócio aplicadas:
/// <list type="bullet">
///   <item>Step-up validity deve estar entre 0 e 1440 minutos (24 horas).</item>
///   <item>Máximo de tentativas deve estar entre 0 e 20.</item>
///   <item>Métodos permitidos não podem ser nulos (pode ser vazio se MFA desabilitado).</item>
///   <item>Se MFA é obrigatório no login, pelo menos um método deve ser configurado.</item>
/// </list>
/// </para>
/// </summary>
public sealed class MfaPolicy : ValueObject
{
    /// <summary>Validade mínima do step-up MFA, em minutos.</summary>
    private const int MinStepUpValidityMinutes = 0;

    /// <summary>Validade máxima do step-up MFA, em minutos (24 horas).</summary>
    private const int MaxStepUpValidityMinutes = 1440;

    /// <summary>Número mínimo de tentativas MFA permitidas.</summary>
    private const int MinMaxAttempts = 0;

    /// <summary>Número máximo de tentativas MFA permitidas.</summary>
    private const int MaxMaxAttempts = 20;

    private MfaPolicy(
        bool requiredOnLogin,
        bool requiredForPrivilegedOps,
        bool requiredForVendorOps,
        string allowedMethods,
        int stepUpValidityMinutes,
        int maxAttempts)
    {
        RequiredOnLogin = requiredOnLogin;
        RequiredForPrivilegedOps = requiredForPrivilegedOps;
        RequiredForVendorOps = requiredForVendorOps;
        AllowedMethods = allowedMethods;
        StepUpValidityMinutes = stepUpValidityMinutes;
        MaxAttempts = maxAttempts;
    }

    /// <summary>Indica se MFA é obrigatório para login inicial.</summary>
    public bool RequiredOnLogin { get; }

    /// <summary>Indica se MFA step-up é exigido para operações privilegiadas.</summary>
    public bool RequiredForPrivilegedOps { get; }

    /// <summary>Indica se MFA step-up é exigido para operações de vendor/licensing.</summary>
    public bool RequiredForVendorOps { get; }

    /// <summary>Métodos MFA aceitos (TOTP, WebAuthn, SMS).</summary>
    public string AllowedMethods { get; }

    /// <summary>Tempo em minutos para validade do step-up MFA antes de re-autenticação.</summary>
    public int StepUpValidityMinutes { get; }

    /// <summary>Número máximo de tentativas MFA antes de lockout temporário.</summary>
    public int MaxAttempts { get; }

    /// <summary>
    /// Cria uma política MFA customizada com parâmetros específicos.
    /// Aplica validações de domínio sobre os limites de step-up e tentativas.
    /// </summary>
    /// <param name="requiredOnLogin">Se MFA é obrigatório no login inicial.</param>
    /// <param name="requiredForPrivilegedOps">Se step-up é exigido para operações privilegiadas.</param>
    /// <param name="requiredForVendorOps">Se step-up é exigido para operações de vendor.</param>
    /// <param name="allowedMethods">Métodos MFA aceitos, separados por vírgula.</param>
    /// <param name="stepUpValidityMinutes">Validade do step-up em minutos (0–1440).</param>
    /// <param name="maxAttempts">Máximo de tentativas antes de lockout (0–20).</param>
    /// <returns>Instância validada de <see cref="MfaPolicy"/>.</returns>
    /// <exception cref="ArgumentNullException">Quando allowedMethods é nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Quando stepUpValidityMinutes ou maxAttempts estão fora dos limites permitidos.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Quando MFA é obrigatório no login mas nenhum método está configurado.
    /// </exception>
    public static MfaPolicy Create(
        bool requiredOnLogin,
        bool requiredForPrivilegedOps,
        bool requiredForVendorOps,
        string allowedMethods = "TOTP,WebAuthn",
        int stepUpValidityMinutes = 15,
        int maxAttempts = 5)
    {
        ArgumentNullException.ThrowIfNull(allowedMethods);

        if (stepUpValidityMinutes < MinStepUpValidityMinutes || stepUpValidityMinutes > MaxStepUpValidityMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepUpValidityMinutes),
                stepUpValidityMinutes,
                $"Step-up validity must be between {MinStepUpValidityMinutes} and {MaxStepUpValidityMinutes} minutes.");
        }

        if (maxAttempts < MinMaxAttempts || maxAttempts > MaxMaxAttempts)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts),
                maxAttempts,
                $"Max attempts must be between {MinMaxAttempts} and {MaxMaxAttempts}.");
        }

        if (requiredOnLogin && string.IsNullOrWhiteSpace(allowedMethods))
        {
            throw new ArgumentException(
                "At least one MFA method must be configured when MFA is required on login.",
                nameof(allowedMethods));
        }

        return new MfaPolicy(
            requiredOnLogin,
            requiredForPrivilegedOps,
            requiredForVendorOps,
            allowedMethods.Trim(),
            stepUpValidityMinutes,
            maxAttempts);
    }

    /// <summary>
    /// Política MFA padrão para ambientes SaaS: MFA obrigatório em tudo.
    /// Sessão step-up de 15 minutos, máximo 5 tentativas, apenas TOTP e WebAuthn.
    /// </summary>
    public static MfaPolicy ForSaaS()
        => new(true, true, true, "TOTP,WebAuthn", 15, 5);

    /// <summary>
    /// Política MFA para self-hosted: MFA apenas para operações privilegiadas e vendor.
    /// Step-up de 30 minutos, máximo 5 tentativas, inclui SMS como método adicional.
    /// </summary>
    public static MfaPolicy ForSelfHosted()
        => new(false, true, true, "TOTP,WebAuthn,SMS", 30, 5);

    /// <summary>
    /// Política MFA para on-premise: MFA opcional, configurável pelo cliente.
    /// Step-up de 60 minutos, até 10 tentativas, todos os métodos disponíveis.
    /// </summary>
    public static MfaPolicy ForOnPremise()
        => new(false, false, false, "TOTP,WebAuthn,SMS", 60, 10);

    /// <summary>
    /// Política MFA desabilitada (apenas para dev/teste).
    /// Nenhum método configurado, nenhum requisito ativo.
    /// </summary>
    public static MfaPolicy Disabled()
        => new(false, false, false, "", 0, 0);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RequiredOnLogin;
        yield return RequiredForPrivilegedOps;
        yield return RequiredForVendorOps;
        yield return AllowedMethods;
        yield return StepUpValidityMinutes;
        yield return MaxAttempts;
    }

    /// <summary>Retorna uma representação textual resumida da política MFA.</summary>
    public override string ToString()
        => $"MfaPolicy(Login={RequiredOnLogin}, Privileged={RequiredForPrivilegedOps}, Vendor={RequiredForVendorOps}, Methods={AllowedMethods})";
}
