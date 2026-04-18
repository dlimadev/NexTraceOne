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
}
