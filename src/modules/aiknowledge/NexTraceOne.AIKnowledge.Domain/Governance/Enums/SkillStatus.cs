namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>Estado de ciclo de vida de uma skill de IA.</summary>
public enum SkillStatus
{
    /// <summary>Rascunho — em criação, não disponível para execução.</summary>
    Draft,

    /// <summary>Ativa — disponível para execução.</summary>
    Active,

    /// <summary>Descontinuada — não deve ser usada em novos fluxos.</summary>
    Deprecated
}
