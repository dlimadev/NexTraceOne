using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo RulesetGovernance com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: RulesetGovernance.{Entidade}.{Descrição}
/// </summary>
public static class RulesetGovernanceErrors
{
    /// <summary>Ruleset não encontrado.</summary>
    public static Error RulesetNotFound(string id)
        => Error.NotFound("RulesetGovernance.Ruleset.NotFound", "Ruleset '{0}' was not found.", id);

    /// <summary>Ruleset já está arquivado.</summary>
    public static Error RulesetAlreadyArchived()
        => Error.Conflict("RulesetGovernance.Ruleset.AlreadyArchived", "This ruleset is already archived.");

    /// <summary>Resultado de linting não encontrado para a release informada.</summary>
    public static Error LintResultNotFound(string releaseId)
        => Error.NotFound("RulesetGovernance.LintResult.NotFound", "Lint result for release '{0}' was not found.", releaseId);

    /// <summary>Binding já existe para esta combinação de ruleset e tipo de ativo.</summary>
    public static Error BindingAlreadyExists(string rulesetId, string assetType)
        => Error.Conflict("RulesetGovernance.RulesetBinding.AlreadyExists", "Binding for ruleset '{0}' and asset type '{1}' already exists.", rulesetId, assetType);

    /// <summary>Conteúdo do ruleset é inválido.</summary>
    public static Error InvalidRulesetContent()
        => Error.Validation("RulesetGovernance.Ruleset.InvalidContent", "The ruleset content is invalid.");
}
