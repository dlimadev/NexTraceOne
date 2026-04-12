namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Estado do ciclo de vida de uma narrativa de anomalia gerada por IA.
/// Draft → Published → Stale (quando dados da anomalia mudam).
/// </summary>
public enum AnomalyNarrativeStatus
{
    /// <summary>Narrativa recém-gerada, ainda não publicada.</summary>
    Draft = 1,

    /// <summary>Narrativa publicada e visível para as personas.</summary>
    Published = 2,

    /// <summary>Narrativa desatualizada — os dados da anomalia mudaram desde a última geração.</summary>
    Stale = 3
}
