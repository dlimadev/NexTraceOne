using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Define o escopo de aplicação de um ruleset de linting de contratos.
/// Permite configurar onde e como o linting é utilizado —
/// globalmente, por organização, domínio, equipa, produto,
/// tipo de serviço, template ou por workflow state.
/// </summary>
public sealed record ContractLintBindingScope(
    /// <summary>Identificador do ruleset a aplicar.</summary>
    Guid RulesetId,
    /// <summary>Linting habilitado neste escopo.</summary>
    bool Enabled,
    /// <summary>Modo de execução para este binding.</summary>
    ContractLintExecutionMode ExecutionMode,
    /// <summary>Comportamento face a violações.</summary>
    ContractLintEnforcementBehavior EnforcementBehavior,
    /// <summary>Escopo: "global", "organization", "domain", "team", "product", "serviceType", "template", "workflowState".</summary>
    string ScopeType,
    /// <summary>Valor do escopo (ex: id da org, nome do domínio, id da equipa).</summary>
    string? ScopeValue = null,
    /// <summary>Acção a que se aplica: "edit", "save", "review", "publish". Null = todas.</summary>
    string? ApplicableAction = null);
