namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Severidade de activação de um guardrail.
/// Determina a urgência da resposta e nível de alerta.
/// </summary>
public enum GuardrailSeverity
{
    /// <summary>Informacional — registo apenas, sem impacto no fluxo.</summary>
    Info,

    /// <summary>Baixa — registo e alerta opcional.</summary>
    Low,

    /// <summary>Média — alerta ao utilizador e auditoria.</summary>
    Medium,

    /// <summary>Alta — bloqueio ou sanitização obrigatória.</summary>
    High,

    /// <summary>Crítica — bloqueio imediato e notificação ao Platform Admin.</summary>
    Critical
}
