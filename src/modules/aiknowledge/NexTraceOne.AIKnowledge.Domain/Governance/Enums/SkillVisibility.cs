namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>Visibilidade de uma skill de IA.</summary>
public enum SkillVisibility
{
    /// <summary>Visível para todos.</summary>
    Public,

    /// <summary>Visível apenas para a equipa proprietária.</summary>
    TeamOnly,

    /// <summary>Visível apenas para o proprietário.</summary>
    Private
}
