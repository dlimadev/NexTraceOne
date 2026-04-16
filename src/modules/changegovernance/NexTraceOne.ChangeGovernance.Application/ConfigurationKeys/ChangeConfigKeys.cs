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

    // ── Parâmetros de Release Intelligence ──────────────────────────────────

    /// <summary>
    /// Score mínimo de confiança (0.0–1.0) exigido para liberar a promoção entre ambientes.
    /// Abaixo deste threshold a promoção é bloqueada automaticamente.
    /// </summary>
    public const string ReleaseMinConfidenceScoreForPromotion = "change.release.min_confidence_score_for_promotion";

    /// <summary>
    /// Duração padrão (em minutos) da janela de observação pós-deploy.
    /// Usado para calcular o período de observation window automático.
    /// </summary>
    public const string ReleaseObservationWindowMinutes = "change.release.observation_window_minutes";

    /// <summary>
    /// Máximo de consumers já migrados (%) que ainda permite rollback seguro.
    /// Acima deste percentual o sistema sinaliza rollback de alto risco.
    /// </summary>
    public const string ReleaseRollbackMaxMigratedConsumersPercent = "change.release.rollback.max_migrated_consumers_percent";

    /// <summary>
    /// Número máximo de dias para gerar evidence pack antes de expirar.
    /// Após este período o evidence pack não é mais exportável sem revalidação.
    /// </summary>
    public const string ReleaseEvidencePackExpiryDays = "change.release.evidence_pack.expiry_days";

    /// <summary>
    /// Habilita a geração automática de release notes por IA ao iniciar a review pós-release.
    /// </summary>
    public const string ReleaseAutoGenerateNotesOnReview = "change.release.auto_generate_notes_on_review";

    /// <summary>
    /// Score mínimo de blast radius (0.0–1.0) que exige aprovação obrigatória do CAB.
    /// </summary>
    public const string ReleaseBlastRadiusCabApprovalThreshold = "change.release.blast_radius.cab_approval_threshold";

    // ── Parâmetros de Release Train ──────────────────────────────────────────

    /// <summary>
    /// Número máximo de itens (commits / work items) permitidos numa única Release Train.
    /// Acima deste limite o sistema avisa o responsável antes de iniciar o trem.
    /// </summary>
    public const string ReleaseTrainMaxItemsPerTrain = "change.release.train.max_items_per_train";

    /// <summary>
    /// Fecha automaticamente a Release Train quando todos os itens tiverem sido deployados.
    /// </summary>
    public const string ReleaseTrainAutoCloseOnAllDeployed = "change.release.train.auto_close_on_all_deployed";

    // ── Parâmetros de Release Notes ──────────────────────────────────────────

    /// <summary>
    /// Identificador do modelo de IA a usar para geração automática de release notes.
    /// Deve coincidir com um model ID registado no Model Registry.
    /// </summary>
    public const string ReleaseNotesAiGenerationModel = "change.release.notes.ai_generation_model";

    // ── Parâmetros de Promotion Gates ────────────────────────────────────────

    /// <summary>
    /// Número máximo de dias que um gate de promoção pode ficar pendente antes de
    /// gerar um alerta automático ao responsável pela release.
    /// </summary>
    public const string ReleaseGatesMaxPendingDaysBeforeAlert = "change.release.gates.max_pending_days_before_alert";

    // ── Parâmetros de Freeze Window ───────────────────────────────────────────

    /// <summary>
    /// Duração padrão (em horas) das freeze windows criadas manualmente.
    /// Pode ser sobreposta por environment override.
    /// </summary>
    public const string ReleaseFreezeWindowDefaultDurationHours = "change.release.freeze_window.default_duration_hours";
}
