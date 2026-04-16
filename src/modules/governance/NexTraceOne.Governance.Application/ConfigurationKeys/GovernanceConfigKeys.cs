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
}
