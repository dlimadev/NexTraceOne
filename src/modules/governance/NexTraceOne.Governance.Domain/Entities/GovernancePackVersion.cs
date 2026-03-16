using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade GovernancePackVersion.
/// Garante que GovernancePackVersionId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record GovernancePackVersionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa uma versão específica de um governance pack, contendo o conjunto
/// concreto de regras (bindings) que compõem essa versão.
/// Cada versão é imutável após criação — alterações resultam numa nova versão.
/// Permite auditoria, rollback e comparação entre versões do pack.
/// </summary>
public sealed class GovernancePackVersion : Entity<GovernancePackVersionId>
{
    /// <summary>
    /// Identificador do governance pack ao qual esta versão pertence.
    /// </summary>
    public GovernancePackId PackId { get; private init; } = default!;

    /// <summary>
    /// Versão semântica desta release do pack (ex: "1.0.0", "2.1.0").
    /// Imutável após criação.
    /// </summary>
    public string Version { get; private init; } = string.Empty;

    /// <summary>
    /// Conjunto de regras (bindings) incluídas nesta versão do pack.
    /// Imutável — alterações criam uma nova versão.
    /// </summary>
    public IReadOnlyList<GovernanceRuleBinding> Rules { get; private init; } = [];

    /// <summary>
    /// Modo de enforcement padrão para regras desta versão.
    /// Pode ser sobrescrito individualmente por cada rule binding.
    /// </summary>
    public EnforcementMode DefaultEnforcementMode { get; private init; }

    /// <summary>
    /// Descrição das alterações introduzidas nesta versão (changelog).
    /// </summary>
    public string? ChangeDescription { get; private init; }

    /// <summary>
    /// Identificador do utilizador que criou esta versão.
    /// </summary>
    public string CreatedBy { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC de criação desta versão.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Data/hora UTC de publicação desta versão.
    /// Null enquanto a versão não tiver sido publicada.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private GovernancePackVersion() { }

    /// <summary>
    /// Cria uma nova versão de governance pack com validação de invariantes.
    /// A versão nasce sem data de publicação — esta é definida quando o pack é publicado.
    /// </summary>
    /// <param name="packId">Identificador do governance pack pai.</param>
    /// <param name="version">Versão semântica (máx. 50 caracteres).</param>
    /// <param name="rules">Conjunto de regras (bindings) para esta versão.</param>
    /// <param name="defaultEnforcementMode">Modo de enforcement padrão.</param>
    /// <param name="changeDescription">Descrição das alterações (máx. 1000 caracteres).</param>
    /// <param name="createdBy">Identificador do criador (máx. 200 caracteres).</param>
    /// <returns>Nova instância válida de GovernancePackVersion.</returns>
    public static GovernancePackVersion Create(
        GovernancePackId packId,
        string version,
        IReadOnlyList<GovernanceRuleBinding> rules,
        EnforcementMode defaultEnforcementMode,
        string? changeDescription,
        string createdBy)
    {
        Guard.Against.Null(packId, nameof(packId));
        Guard.Against.NullOrWhiteSpace(version, nameof(version));
        Guard.Against.StringTooLong(version, 50, nameof(version));
        Guard.Against.Null(rules, nameof(rules));
        Guard.Against.EnumOutOfRange(defaultEnforcementMode, nameof(defaultEnforcementMode));

        if (changeDescription is not null)
            Guard.Against.StringTooLong(changeDescription, 1000, nameof(changeDescription));

        Guard.Against.NullOrWhiteSpace(createdBy, nameof(createdBy));
        Guard.Against.StringTooLong(createdBy, 200, nameof(createdBy));

        return new GovernancePackVersion
        {
            Id = new GovernancePackVersionId(Guid.NewGuid()),
            PackId = packId,
            Version = version.Trim(),
            Rules = rules,
            DefaultEnforcementMode = defaultEnforcementMode,
            ChangeDescription = changeDescription?.Trim(),
            CreatedBy = createdBy.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            PublishedAt = null
        };
    }
}
