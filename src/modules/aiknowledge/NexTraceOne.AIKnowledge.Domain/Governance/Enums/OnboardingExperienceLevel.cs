namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Nível de experiência do utilizador no fluxo de onboarding assistido por IA.
/// </summary>
public enum OnboardingExperienceLevel
{
    /// <summary>Profissional júnior — precisa de orientação detalhada.</summary>
    Junior = 1,

    /// <summary>Profissional de nível intermédio.</summary>
    Mid = 2,

    /// <summary>Profissional sénior — orientação mais focada.</summary>
    Senior = 3,

    /// <summary>Especialista — onboarding acelerado.</summary>
    Expert = 4
}
