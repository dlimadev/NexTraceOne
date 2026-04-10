namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Estado do ciclo de vida de uma narrativa de incidente gerada por IA.
/// Draft → Published → Stale (quando dados do incidente mudam após a geração).
/// </summary>
public enum NarrativeStatus
{
    /// <summary>Narrativa recém-gerada, ainda não publicada.</summary>
    Draft = 1,

    /// <summary>Narrativa publicada e visível para as personas.</summary>
    Published = 2,

    /// <summary>Narrativa desatualizada — os dados do incidente mudaram desde a última geração.</summary>
    Stale = 3
}
