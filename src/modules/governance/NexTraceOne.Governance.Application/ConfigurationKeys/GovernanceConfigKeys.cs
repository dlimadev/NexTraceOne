namespace NexTraceOne.Governance.Application.ConfigurationKeys;

/// <summary>
/// Constantes para as chaves de configuração do módulo Governance.
/// Centraliza os literais de string utilizados em consultas à IConfigurationResolutionService,
/// eliminando duplicação e erros de digitação.
/// </summary>
public static class GovernanceConfigKeys
{
    /// <summary>Habilita o processo de Change Advisory Board (CAB) para aprovação de mudanças.</summary>
    public const string ChangeAdvisoryBoardEnabled = "governance.change_advisory_board.enabled";

    /// <summary>Condições que disparam a obrigatoriedade de revisão pelo CAB (JSON array).</summary>
    public const string ChangeAdvisoryBoardTriggerConditions = "governance.change_advisory_board.trigger_conditions";

    /// <summary>Lista de membros do CAB (JSON array de user IDs ou emails).</summary>
    public const string ChangeAdvisoryBoardMembers = "governance.change_advisory_board.members";

    /// <summary>Habilita o princípio dos quatro olhos para aprovações críticas.</summary>
    public const string FourEyesPrincipleEnabled = "governance.four_eyes_principle.enabled";

    /// <summary>Ações que requerem validação por dois aprovadores distintos (JSON array).</summary>
    public const string FourEyesPrincipleActions = "governance.four_eyes_principle.actions";

    /// <summary>Habilita a remediação automática de incumprimentos de compliance.</summary>
    public const string ComplianceAutoRemediationEnabled = "governance.compliance.auto_remediation.enabled";

    /// <summary>Framework de compliance activo para o tenant (ex: ISO27001, SOC2, PCI-DSS).</summary>
    public const string ComplianceFramework = "governance.compliance.framework";

    // ── FinOps ─────────────────────────────────────────────────────────

    /// <summary>Código ISO 4217 da moeda padrão para apresentação de custos FinOps (ex: USD, EUR, BRL).</summary>
    public const string FinOpsCurrency = "finops.budget.default_currency";

    /// <summary>Habilita o gate de orçamento para promoções de release a produção.</summary>
    public const string FinOpsBudgetGateEnabled = "finops.release.budget_gate.enabled";

    /// <summary>
    /// Quando true, bloqueia a promoção se o custo da release ultrapassar o orçamento configurado.
    /// Quando false, apenas emite um aviso mas permite prosseguir.
    /// </summary>
    public const string FinOpsBudgetGateBlockOnExceed = "finops.release.budget_gate.block_on_exceed";

    /// <summary>
    /// Quando true e o gate está em modo bloqueio, permite override mediante aprovação.
    /// Um aprovador designado pode autorizar a promoção mesmo com orçamento excedido.
    /// </summary>
    public const string FinOpsBudgetGateRequireApproval = "finops.release.budget_gate.require_approval";

    /// <summary>Lista JSON de utilizadores/grupos com permissão para aprovar overrides de orçamento FinOps.</summary>
    public const string FinOpsBudgetGateApprovers = "finops.release.budget_gate.approvers";

    /// <summary>
    /// Thresholds de alerta de orçamento multi-tier (JSON array com percent, severity, action).
    /// Substitui a chave simples <c>finops.budget_alert_threshold</c>.
    /// </summary>
    public const string FinOpsBudgetAlertThresholds = "finops.budget.alert_thresholds";

    /// <summary>Orçamento mensal por equipa (JSON object: teamId → {monthlyBudget, currency}).</summary>
    public const string FinOpsBudgetByTeam = "finops.budget.by_team";

    /// <summary>Restrições de orçamento por ambiente (JSON object: environment → {monthlyBudget, currency}).</summary>
    public const string FinOpsBudgetByEnvironment = "finops.budget.by_environment";

    /// <summary>Habilita a detecção de anomalias de custo.</summary>
    public const string FinOpsAnomalyDetectionEnabled = "finops.anomaly.detection_enabled";

    /// <summary>Thresholds de detecção de anomalias (JSON: {warning, high, critical} em % de desvio).</summary>
    public const string FinOpsAnomalyThresholds = "finops.anomaly.thresholds";

    /// <summary>Janela de comparação em dias para deteção de anomalias de custo.</summary>
    public const string FinOpsAnomalyComparisonWindowDays = "finops.anomaly.comparison_window_days";

    /// <summary>Habilita a detecção de sinais de desperdício operacional.</summary>
    public const string FinOpsWasteDetectionEnabled = "finops.waste.detection_enabled";

    /// <summary>Thresholds para deteção de desperdício (JSON: percentileThreshold, overProvisionedCostRatio, idleCostlyRatio).</summary>
    public const string FinOpsWasteThresholds = "finops.waste.thresholds";

    /// <summary>Categorias de desperdício activas para classificação (JSON array de strings).</summary>
    public const string FinOpsWasteCategories = "finops.waste.categories";

    /// <summary>Política de recomendações financeiras (JSON: minSavingsThreshold, savingsRatePct, showInDashboard, notifyOnHighSavings).</summary>
    public const string FinOpsRecommendationPolicy = "finops.recommendation.policy";

    /// <summary>Política de notificações FinOps (JSON: notifyOnAnomaly, digestFrequency, recipients).</summary>
    public const string FinOpsNotificationPolicy = "finops.notification.policy";

    /// <summary>Habilita a apresentação de custos por serviço, equipa e domínio (showback).</summary>
    public const string FinOpsShowbackEnabled = "finops.showback.enabled";

    /// <summary>Habilita o modelo de chargeback — custos debitados às equipas responsáveis.</summary>
    public const string FinOpsChargebackEnabled = "finops.chargeback.enabled";

    /// <summary>
    /// Percentagem de variação de custo que classifica a tendência como Declining ou Improving.
    /// Default: 5.0 (variação &gt; 5% → Declining; &lt; -5% → Improving).
    /// </summary>
    public const string FinOpsEfficiencyTrendThresholdPct = "finops.efficiency.trend_threshold_pct";

    /// <summary>Bandas monetárias absolutas para classificação de eficiência de custo por serviço (JSON: {Wasteful, Inefficient, Acceptable}).</summary>
    public const string FinOpsEfficiencyCostBands = "finops.efficiency.cost_bands";

    /// <summary>Thresholds de burn rate para classificação de confiabilidade (JSON: {elevated, critical}).</summary>
    public const string FinOpsEfficiencyBurnRateThresholds = "finops.efficiency.burn_rate_thresholds";

    // ── Wave V3.4 — Notebooks &amp; AI Composer (sort 11000–11060) ─────────────────

    /// <summary>Número máximo de células por notebook (default: 50).</summary>
    public const string NotebookMaxCells = "notebook.max_cells";

    /// <summary>Número máximo de notebooks por tenant (default: 500).</summary>
    public const string NotebookMaxPerTenant = "notebook.max_per_tenant";

    /// <summary>Habilita a execução de células Query (NQL) nas notebooks.</summary>
    public const string NotebookQueryExecutionEnabled = "notebook.query_execution.enabled";

    /// <summary>Número máximo de widgets propostos pelo AI Composer por chamada (default: 12).</summary>
    public const string AiComposerMaxWidgets = "notebook.ai_composer.max_widgets";

    /// <summary>Modelo de IA preferido para o AI Dashboard Composer (default: claude-sonnet-4-6).</summary>
    public const string AiComposerModel = "notebook.ai_composer.model";

    /// <summary>Habilita o AI Dashboard Composer para o tenant.</summary>
    public const string AiComposerEnabled = "notebook.ai_composer.enabled";

    // ── Wave V3.5 — Frontend Platform Uplift (sort 11100–11160) ──────────────

    /// <summary>Habilita a Command Palette global (Ctrl+K / Cmd+K) no frontend.</summary>
    public const string CommandPaletteEnabled = "frontend.command_palette.enabled";

    /// <summary>Número máximo de resultados de busca semântica na Command Palette (default: 20).</summary>
    public const string CommandPaletteMaxResults = "frontend.command_palette.max_results";

    /// <summary>Habilita View Transitions API no frontend para transições de rota premium.</summary>
    public const string ViewTransitionsEnabled = "frontend.view_transitions.enabled";

    /// <summary>Budget de tamanho máximo do bundle JS em KB (default: 800).</summary>
    public const string PerfBudgetBundleSizeKb = "frontend.perf_budget.bundle_size_kb";

    /// <summary>Threshold de LCP (Largest Contentful Paint) em ms para CI pass/fail (default: 2500).</summary>
    public const string PerfBudgetLcpMs = "frontend.perf_budget.lcp_ms";

    /// <summary>Habilita telemetria de UX (eventos de uso por módulo alimentando ProductAnalytics).</summary>
    public const string UxTelemetryEnabled = "frontend.ux_telemetry.enabled";
}
