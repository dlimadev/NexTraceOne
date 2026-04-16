namespace NexTraceOne.Catalog.Application.ConfigurationKeys;

/// <summary>
/// Constantes para as chaves de configuração do módulo Catalog.
/// Centraliza os literais de string utilizados em consultas à IConfigurationResolutionService,
/// eliminando duplicação e erros de digitação.
/// </summary>
public static class CatalogConfigKeys
{
    /// <summary>Bloqueia a publicação de contratos quando existem erros de linting.</summary>
    public const string ContractValidationBlockOnLintErrors = "catalog.contract.validation.block_on_lint_errors";

    /// <summary>Exige a presença de pelo menos um exemplo (payload) antes de publicar o contrato.</summary>
    public const string ContractPublicationRequireExamples = "catalog.contract.publication.require_examples";

    /// <summary>Exige aprovação explícita antes de criar novos contratos.</summary>
    public const string ContractCreationApprovalRequired = "catalog.contract.creation.approval_required";

    /// <summary>Bloqueia deploys quando a release contém breaking changes em contratos não resolvidos.</summary>
    public const string ContractBreakingChangeBlockDeploy = "catalog.contract.breaking_change.block_deploy";

    /// <summary>Exige aprovação explícita antes de registar novos serviços no catálogo.</summary>
    public const string ServiceCreationApprovalRequired = "catalog.service.creation.approval_required";
}
