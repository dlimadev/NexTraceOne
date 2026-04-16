namespace NexTraceOne.ChangeGovernance.Application.ConfigurationKeys;

/// <summary>
/// Constantes para as chaves de configuração do módulo ChangeGovernance.
/// Centraliza os literais de string utilizados em consultas à IConfigurationResolutionService,
/// eliminando duplicação e erros de digitação.
/// </summary>
public static class ChangeConfigKeys
{
    /// <summary>Exige aprovação explícita da release antes de permitir deploy.</summary>
    public const string DeployRequireReleaseApproval = "change.deploy.require_release_approval";

    /// <summary>
    /// Verificações pré-deploy obrigatórias (JSON object: chave=nome, valor=boolean).
    /// Exemplo: {"contract_compliance": true, "security_scan": true}.
    /// </summary>
    public const string DeployPreDeployChecks = "change.deploy.pre_deploy_checks";

    /// <summary>Habilita a validação externa de CI/CD como requisito de deploy.</summary>
    public const string ReleaseExternalValidationEnabled = "change.release.external_validation.enabled";

    // ── Chave de contrato cruzado (pertence ao módulo Catalog) ──────────────
    // Repetida aqui para evitar dependência compile-time de Catalog.Application
    // em módulos de Change — a chave deve ser mantida sincronizada manualmente.

    /// <summary>
    /// Bloqueia deploys quando a release contém breaking changes em contratos.
    /// (Chave originária do módulo Catalog — ver CatalogConfigKeys.ContractBreakingChangeBlockDeploy)
    /// </summary>
    public const string CatalogContractBreakingChangeBlockDeploy = "catalog.contract.breaking_change.block_deploy";
}
