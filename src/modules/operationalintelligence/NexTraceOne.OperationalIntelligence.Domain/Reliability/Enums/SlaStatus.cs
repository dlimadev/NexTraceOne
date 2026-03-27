namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Estado de conformidade de um SLA (Service Level Agreement).
/// Indica se o contrato de nível de serviço está a ser respeitado.
/// </summary>
public enum SlaStatus
{
    /// <summary>SLA ativo e dentro dos termos acordados.</summary>
    Active = 0,

    /// <summary>SLA em risco — métricas a aproximar-se dos limites contratuais.</summary>
    AtRisk = 1,

    /// <summary>SLA violado — termos contratuais não cumpridos no período de medição.</summary>
    Breached = 2
}
