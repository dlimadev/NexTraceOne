using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Identity.Domain.ValueObjects;

/// <summary>
/// Value Object que representa o modelo de deployment da plataforma NexTraceOne.
/// Cada tenant opera sob um modelo específico que determina restrições de autenticação,
/// armazenamento de dados, políticas de segurança e capacidades disponíveis.
/// <para>
/// Modelos suportados:
/// <list type="bullet">
///   <item><term>SaaS</term> — plataforma gerenciada na nuvem pelo fornecedor;</item>
///   <item><term>SelfHosted</term> — instalação gerenciada pelo cliente em infraestrutura própria;</item>
///   <item><term>OnPremise</term> — instalação air-gapped ou altamente restrita, sem conectividade externa.</item>
/// </list>
/// </para>
/// A distinção entre SelfHosted e OnPremise impacta funcionalidades como
/// verificação de licença online, atualização automática, telemetria e
/// integração com provedores de identidade externos.
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
    /// Instalação gerenciada pelo cliente em infraestrutura própria.
    /// Suporta autenticação híbrida e conectividade externa para licenças e atualizações.
    /// </summary>
    public static DeploymentModel SelfHosted => new("SelfHosted");

    /// <summary>
    /// Instalação air-gapped ou altamente restrita, sem conectividade externa.
    /// Licenciamento offline obrigatório, sem telemetria, autenticação local disponível.
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
    /// Indica se o modelo permite conectividade externa para verificação de licença,
    /// atualizações automáticas e telemetria.
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
