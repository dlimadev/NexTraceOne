using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.ValueObjects;

/// <summary>
/// Value Object que representa o modo de conectividade e operação de um tenant.
/// Determina restrições de autenticação, políticas de segurança e capacidades disponíveis
/// com base no contexto de conectividade do ambiente.
/// <para>
/// Modos suportados:
/// <list type="bullet">
///   <item><term>SaaS</term> — ambiente gerenciado com conectividade externa plena;</item>
///   <item><term>SelfHosted</term> — ambiente com conectividade externa disponível, gerenciado pelo operador;</item>
///   <item><term>OnPremise</term> — ambiente air-gapped ou altamente restrito, sem conectividade externa.</item>
/// </list>
/// </para>
/// A distinção entre os modos impacta autenticação federada, telemetria e integração com provedores externos.
/// </summary>
public sealed class DeploymentModel : ValueObject
{
    /// <summary>Valores válidos aceitos pelo factory method.</summary>
    private static readonly HashSet<string> ValidModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "SaaS",
        "SelfHosted",
        "OnPremise"
    };

    private DeploymentModel(string value)
    {
        Value = value;
    }

    /// <summary>Valor normalizado do modelo de deployment.</summary>
    public string Value { get; }

    /// <summary>
    /// Plataforma gerenciada na nuvem pelo fornecedor.
    /// Autenticação federada obrigatória, MFA obrigatório, telemetria ativa.
    /// </summary>
    public static DeploymentModel SaaS => new("SaaS");

    /// <summary>
    /// Ambiente gerenciado pelo operador com conectividade externa disponível.
    /// Suporta autenticação híbrida e integração com provedores de identidade externos.
    /// </summary>
    public static DeploymentModel SelfHosted => new("SelfHosted");

    /// <summary>
    /// Ambiente air-gapped ou altamente restrito, sem conectividade externa.
    /// Sem telemetria remota; autenticação local disponível como método primário.
    /// </summary>
    public static DeploymentModel OnPremise => new("OnPremise");

    /// <summary>
    /// Cria um <see cref="DeploymentModel"/> a partir de uma string validada.
    /// A comparação é case-insensitive; o valor armazenado é normalizado para o formato canônico.
    /// </summary>
    /// <param name="value">Texto representando o modelo de deployment (SaaS, SelfHosted, OnPremise).</param>
    /// <returns>Instância validada de <see cref="DeploymentModel"/>.</returns>
    /// <exception cref="ArgumentException">Quando o valor não corresponde a nenhum modelo válido.</exception>
    public static DeploymentModel From(string value)
    {
        var trimmed = Guard.Against.NullOrWhiteSpace(value).Trim();

        var canonical = ValidModels.FirstOrDefault(v => v.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

        if (canonical is null)
        {
            throw new ArgumentException(
                $"Invalid deployment model '{value}'. Valid values are: {string.Join(", ", ValidModels)}.",
                nameof(value));
        }

        return new DeploymentModel(canonical);
    }

    /// <summary>Indica se o modelo é SaaS (gerenciado pelo fornecedor).</summary>
    public bool IsSaaS => Value == "SaaS";

    /// <summary>Indica se o modelo é SelfHosted (gerenciado pelo cliente com conectividade).</summary>
    public bool IsSelfHosted => Value == "SelfHosted";

    /// <summary>Indica se o modelo é OnPremise (air-gapped, sem conectividade externa).</summary>
    public bool IsOnPremise => Value == "OnPremise";

    /// <summary>
    /// Indica se o modo permite conectividade externa para telemetria
    /// e integração com provedores de identidade externos.
    /// </summary>
    public bool AllowsExternalConnectivity => IsSaaS || IsSelfHosted;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>Retorna o modelo de deployment como string.</summary>
    public override string ToString() => Value;
}
