using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Estado de saúde de um serviço em runtime, determinado pela análise
/// combinada de taxa de erro, latência e métricas de capacidade.
/// Utilizado pelo <see cref="RuntimeSnapshot"/>
/// para classificar automaticamente a condição operacional do serviço.
/// </summary>
public enum HealthStatus
{
    /// <summary>Serviço operando dentro dos parâmetros normais — sem anomalias detectadas.</summary>
    Healthy = 0,

    /// <summary>Serviço apresenta degradação parcial — métricas fora dos limiares ideais mas ainda funcional.</summary>
    Degraded = 1,

    /// <summary>Serviço com falhas significativas — taxa de erro ou latência acima dos limiares críticos.</summary>
    Unhealthy = 2,

    /// <summary>Estado de saúde indeterminado — dados insuficientes para classificação.</summary>
    Unknown = 3
}
