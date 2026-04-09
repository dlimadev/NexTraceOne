namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Estado do ciclo de vida das release notes geradas por IA.
/// Draft → Published → Archived.
/// </summary>
public enum ReleaseNotesStatus
{
    /// <summary>Release notes recém-geradas, ainda não publicadas.</summary>
    Draft = 1,

    /// <summary>Release notes publicadas e visíveis para as personas.</summary>
    Published = 2,

    /// <summary>Release notes arquivadas (versão anterior substituída).</summary>
    Archived = 3
}
