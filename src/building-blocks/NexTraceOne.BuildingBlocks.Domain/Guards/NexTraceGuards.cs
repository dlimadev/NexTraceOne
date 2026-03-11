using Ardalis.GuardClauses;
using System.Text.RegularExpressions;

namespace NexTraceOne.BuildingBlocks.Domain.Guards;

/// <summary>
/// Extensões de Guard específicas do domínio NexTraceOne.
/// Complementam as guards genéricas do Ardalis.GuardClauses com
/// validações de negócio recorrentes na plataforma.
/// Uso: Guard.Against.InvalidSemanticVersion(version);
/// </summary>
public static class NexTraceGuards
{
    private static readonly Regex SemanticVersionRegex = new(
        "^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-((?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+([0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private static readonly HashSet<string> GovernedEnvironments =
    [
        "Integration",
        "QA",
        "UAT",
        "Staging",
        "Production"
    ];

    /// <summary>
    /// Verifica que a string representa uma versão semântica válida (SemVer 2.0).
    /// Lança ArgumentException se inválida, convertida para HTTP 400 pelo middleware.
    /// </summary>
    public static string InvalidSemanticVersion(this IGuardClause _, string version, string paramName = "version")
    {
        var normalizedVersion = Guard.Against.NullOrWhiteSpace(version, paramName).Trim();

        if (!SemanticVersionRegex.IsMatch(normalizedVersion))
        {
            throw new ArgumentException("Semantic version must follow SemVer 2.0.", paramName);
        }

        return normalizedVersion;
    }

    /// <summary>
    /// Verifica que o ambiente informado é um ambiente governado válido.
    /// Ambientes não governados (ex: Development) são rejeitados.
    /// </summary>
    public static string UngovernedEnvironment(this IGuardClause _, string environment, string paramName = "environment")
    {
        var normalizedEnvironment = Guard.Against.NullOrWhiteSpace(environment, paramName).Trim();

        if (!GovernedEnvironments.Contains(normalizedEnvironment))
        {
            throw new ArgumentException("Environment must be one of the governed platform environments.", paramName);
        }

        return normalizedEnvironment;
    }

    /// <summary>
    /// Verifica que o TenantId não é Guid.Empty.
    /// Requisições sem tenant ativo são rejeitadas.
    /// </summary>
    public static Guid EmptyTenantId(this IGuardClause _, Guid tenantId, string paramName = "tenantId")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", paramName);
        return tenantId;
    }
}
