namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Severidade de um drift (desvio) detectado entre a baseline esperada
/// e os valores reais de runtime. Determinada automaticamente pelo percentual
/// de desvio via <see cref="NexTraceOne.RuntimeIntelligence.Domain.Entities.DriftFinding.CalculateSeverity"/>.
/// </summary>
public enum DriftSeverity
{
    /// <summary>Desvio leve (até 10%) — informativo, sem ação imediata necessária.</summary>
    Low = 0,

    /// <summary>Desvio moderado (10–25%) — requer monitoramento e possível investigação.</summary>
    Medium = 1,

    /// <summary>Desvio significativo (25–50%) — requer investigação e possível intervenção.</summary>
    High = 2,

    /// <summary>Desvio crítico (acima de 50%) — requer ação imediata, possível impacto em produção.</summary>
    Critical = 3
}
