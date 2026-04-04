namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Resultado final do Post-Incident Review.
/// </summary>
public enum PostIncidentReviewOutcome
{
    /// <summary>Ainda não determinado.</summary>
    Pending = 0,

    /// <summary>Causa raiz identificada com ações preventivas definidas.</summary>
    RootCauseIdentified = 1,

    /// <summary>Causa raiz parcialmente identificada; investigação adicional pode ser necessária.</summary>
    PartiallyIdentified = 2,

    /// <summary>Causa raiz não identificada; incidente considerado inconclusivo.</summary>
    Inconclusive = 3,

    /// <summary>Incidente causado por fatores externos fora do controlo da equipa.</summary>
    ExternalCause = 4
}
