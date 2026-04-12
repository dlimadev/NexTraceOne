namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Estado do ciclo de vida de uma sessão de onboarding assistido por IA.
/// </summary>
public enum OnboardingSessionStatus
{
    /// <summary>Sessão em curso — utilizador ainda a completar checklist.</summary>
    Active = 1,

    /// <summary>Sessão concluída com sucesso.</summary>
    Completed = 2,

    /// <summary>Sessão abandonada antes da conclusão.</summary>
    Abandoned = 3
}
