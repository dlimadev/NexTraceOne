namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Estado de uma observação de SLO face ao objetivo definido.
/// Wave J.2 — SLO Tracking.
/// </summary>
public enum SloObservationStatus
{
    /// <summary>Valor observado cumpre o objetivo de SLO.</summary>
    Met = 0,

    /// <summary>Valor observado está dentro do intervalo de alerta (dentro de 10% do alvo).</summary>
    Warning = 1,

    /// <summary>Valor observado viola o objetivo de SLO.</summary>
    Breached = 2,
}
