namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Tipo de SLO (Service Level Objective).
/// Define qual indicador operacional é alvo do objetivo de nível de serviço.
/// </summary>
public enum SloType
{
    /// <summary>Disponibilidade — percentagem de tempo em que o serviço está operacional.</summary>
    Availability = 0,

    /// <summary>Latência — tempo de resposta dentro de um limiar definido (ex: P99 &lt; 200ms).</summary>
    Latency = 1,

    /// <summary>Taxa de erro — percentagem de requisições que resultam em erro abaixo de um limiar.</summary>
    ErrorRate = 2,

    /// <summary>Throughput — volume mínimo de requisições processadas com sucesso por período.</summary>
    Throughput = 3
}
