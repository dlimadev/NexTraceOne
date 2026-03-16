using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Value object que representa a ligação de uma regra a um governance pack version.
/// Contém os metadados da regra no contexto específico do pack, incluindo
/// a categoria, o modo de enforcement padrão e se a regra é obrigatória.
/// Imutável por natureza — alterações criam uma nova versão do pack.
/// </summary>
/// <param name="RuleId">Identificador único da regra dentro do pack (ex: "contract-must-have-examples").</param>
/// <param name="RuleName">Nome legível da regra (ex: "Contract Must Have Examples").</param>
/// <param name="Description">Descrição opcional do propósito e critérios da regra.</param>
/// <param name="Category">Categoria da regra, determinando o domínio de governança abrangido.</param>
/// <param name="DefaultEnforcementMode">Modo de enforcement padrão da regra neste pack.</param>
/// <param name="IsRequired">Indica se a regra é obrigatória e não pode ser desativada via waiver.</param>
public sealed record GovernanceRuleBinding(
    string RuleId,
    string RuleName,
    string? Description,
    GovernanceRuleCategory Category,
    EnforcementMode DefaultEnforcementMode,
    bool IsRequired);
