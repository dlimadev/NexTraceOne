namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>Tipo de ownership de uma skill de IA.</summary>
public enum SkillOwnershipType
{
    /// <summary>Skill oficial da plataforma.</summary>
    System,

    /// <summary>Skill criada por uma organização (tenant).</summary>
    Tenant,

    /// <summary>Skill criada por uma equipa.</summary>
    Team,

    /// <summary>Skill criada por um utilizador.</summary>
    User,

    /// <summary>Skill partilhada pela comunidade.</summary>
    Community
}
