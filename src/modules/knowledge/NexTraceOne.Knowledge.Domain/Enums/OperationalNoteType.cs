namespace NexTraceOne.Knowledge.Domain.Enums;

/// <summary>
/// Tipo funcional de uma nota operacional para classificar sua origem e propósito.
/// </summary>
public enum OperationalNoteType
{
    /// <summary>Observação operacional geral.</summary>
    Observation,

    /// <summary>Passo de mitigação/tentativa de resolução.</summary>
    Mitigation,

    /// <summary>Registo de decisão operacional.</summary>
    Decision,

    /// <summary>Hipótese investigativa.</summary>
    Hypothesis,

    /// <summary>Lição aprendida ou follow-up.</summary>
    FollowUp
}
