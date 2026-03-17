using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Errors;

namespace NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

/// <summary>
/// Aggregate Root que representa um ruleset de governança (conjunto de regras de linting).
/// Pode ser do tipo Custom (criado pelo usuário) ou Default (pré-instalado pelo sistema).
/// Gerencia ciclo de vida: ativo, arquivado e atualização de conteúdo.
/// </summary>
public sealed class Ruleset : AuditableEntity<RulesetId>
{
    private Ruleset() { }

    /// <summary>Nome do ruleset (máx. 200 caracteres).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do ruleset (máx. 2000 caracteres).</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Conteúdo do ruleset em formato JSON ou YAML.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Tipo do ruleset: Custom ou Default.</summary>
    public RulesetType RulesetType { get; private set; }

    /// <summary>Indica se o ruleset está ativo para uso em linting.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC em que o ruleset foi criado.</summary>
    public DateTimeOffset RulesetCreatedAt { get; private set; }

    /// <summary>
    /// Cria um novo ruleset com validações de domínio.
    /// </summary>
    public static Ruleset Create(
        string name,
        string description,
        string content,
        RulesetType rulesetType,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(content);

        return new Ruleset
        {
            Id = RulesetId.New(),
            Name = name,
            Description = description ?? string.Empty,
            Content = content,
            RulesetType = rulesetType,
            IsActive = true,
            RulesetCreatedAt = createdAt
        };
    }

    /// <summary>
    /// Arquiva o ruleset (soft-disable), impedindo uso em novas execuções de linting.
    /// Retorna falha se o ruleset já estiver arquivado.
    /// </summary>
    public Result<MediatR.Unit> Archive()
    {
        if (!IsActive)
            return RulesetGovernanceErrors.RulesetAlreadyArchived();

        IsActive = false;
        return MediatR.Unit.Value;
    }

    /// <summary>Reativa um ruleset previamente arquivado.</summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>Atualiza o conteúdo do ruleset.</summary>
    public void UpdateContent(string content)
    {
        Guard.Against.NullOrWhiteSpace(content);
        Content = content;
    }
}

/// <summary>Tipo de ruleset: personalizado ou padrão do sistema.</summary>
public enum RulesetType
{
    /// <summary>Ruleset criado pelo usuário.</summary>
    Custom = 0,

    /// <summary>Ruleset pré-instalado pelo sistema.</summary>
    Default = 1
}

/// <summary>Identificador fortemente tipado de Ruleset.</summary>
public sealed record RulesetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RulesetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RulesetId From(Guid id) => new(id);
}
