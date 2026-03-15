namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Severidade do incidente operacional.
/// Determina prioridade de resposta e visibilidade por persona.
/// </summary>
public enum IncidentSeverity
{
    /// <summary>Alerta informativo — sem impacto imediato confirmado.</summary>
    Warning = 0,

    /// <summary>Impacto menor — funcionalidade secundária afetada.</summary>
    Minor = 1,

    /// <summary>Impacto significativo — funcionalidade principal parcialmente comprometida.</summary>
    Major = 2,

    /// <summary>Impacto crítico — serviço indisponível ou gravemente comprometido.</summary>
    Critical = 3
}
