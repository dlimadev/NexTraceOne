namespace NexTraceOne.Contracts.Domain.Enums;

/// <summary>
/// Decisão de revisão de um draft de contrato.
/// </summary>
public enum ReviewDecision
{
    /// <summary>Aprovado pelo revisor.</summary>
    Approved = 0,

    /// <summary>Rejeitado pelo revisor, necessita ajustes.</summary>
    Rejected = 1
}
