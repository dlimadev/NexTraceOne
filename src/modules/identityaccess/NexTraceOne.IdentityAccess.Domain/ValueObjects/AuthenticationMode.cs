using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.ValueObjects;

/// <summary>
/// Value Object que representa o modo primário de autenticação de um tenant.
/// Determina como os utilizadores se autenticam na plataforma e quais fluxos
/// de login estão disponíveis.
/// <para>
/// Modos suportados:
/// <list type="bullet">
///   <item>
///     <term>Federated</term> — autenticação exclusivamente via OIDC/SSO externo.
///     Utilizado em cenários SaaS e corporativos com IdP centralizado.
///     Não permite login local com senha.
///   </item>
///   <item>
///     <term>Local</term> — autenticação exclusivamente via credenciais locais (email + senha).
///     Utilizado em contextos sem acesso a provedores de identidade externos.
///   </item>
///   <item>
///     <term>Hybrid</term> — autenticação federada preferencial com fallback para login local.
///     Garante contingência quando o IdP externo fica indisponível.
///   </item>
/// </list>
/// </para>
/// A escolha do modo de autenticação impacta diretamente quais endpoints de login
/// ficam ativos, quais políticas de senha se aplicam e como sessões são gerenciadas.
/// </summary>
public sealed class AuthenticationMode : ValueObject
{
    /// <summary>Valores válidos aceitos pelo factory method.</summary>
    private static readonly HashSet<string> ValidModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Federated",
        "Local",
        "Hybrid"
    };

    private AuthenticationMode(string value)
    {
        Value = value;
    }

    /// <summary>Valor normalizado do modo de autenticação.</summary>
    public string Value { get; }

    /// <summary>
    /// Autenticação exclusivamente via OIDC/SSO externo.
    /// Não permite login local com senha — todo acesso depende do Identity Provider configurado.
    /// </summary>
    public static AuthenticationMode Federated => new("Federated");

    /// <summary>
    /// Autenticação exclusivamente via credenciais locais (email + senha).
    /// Indicado para contextos sem acesso a provedores de identidade externos.
    /// </summary>
    public static AuthenticationMode Local => new("Local");

    /// <summary>
    /// Autenticação federada preferencial com fallback para login local.
    /// Garante contingência quando o IdP externo fica indisponível.
    /// </summary>
    public static AuthenticationMode Hybrid => new("Hybrid");

    /// <summary>
    /// Cria um <see cref="AuthenticationMode"/> a partir de uma string validada.
    /// A comparação é case-insensitive; o valor armazenado é normalizado para o formato canônico.
    /// </summary>
    /// <param name="value">Texto representando o modo de autenticação (Federated, Local, Hybrid).</param>
    /// <returns>Instância validada de <see cref="AuthenticationMode"/>.</returns>
    /// <exception cref="ArgumentException">Quando o valor não corresponde a nenhum modo válido.</exception>
    public static AuthenticationMode From(string value)
    {
        var trimmed = Guard.Against.NullOrWhiteSpace(value).Trim();

        var canonical = ValidModes.FirstOrDefault(v => v.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

        if (canonical is null)
        {
            throw new ArgumentException(
                $"Invalid authentication mode '{value}'. Valid values are: {string.Join(", ", ValidModes)}.",
                nameof(value));
        }

        return new AuthenticationMode(canonical);
    }

    /// <summary>Indica se o modo é exclusivamente federado (OIDC/SSO).</summary>
    public bool IsFederated => Value == "Federated";

    /// <summary>Indica se o modo é exclusivamente local (credenciais locais).</summary>
    public bool IsLocal => Value == "Local";

    /// <summary>Indica se o modo é híbrido (federado com fallback local).</summary>
    public bool IsHybrid => Value == "Hybrid";

    /// <summary>
    /// Indica se o modo permite login local com senha,
    /// seja como método primário (Local) ou como fallback (Hybrid).
    /// </summary>
    public bool SupportsLocalLogin => IsLocal || IsHybrid;

    /// <summary>
    /// Indica se o modo permite login federado via OIDC/SSO,
    /// seja como método primário (Federated) ou preferencial (Hybrid).
    /// </summary>
    public bool SupportsFederatedLogin => IsFederated || IsHybrid;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>Retorna o modo de autenticação como string.</summary>
    public override string ToString() => Value;
}
