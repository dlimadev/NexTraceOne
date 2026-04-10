namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Estado de uma negociação cross-team de contrato.
/// Segue o fluxo: Draft → InReview → Negotiating → Approved/Rejected.
/// </summary>
public enum NegotiationStatus
{
    /// <summary>Rascunho inicial — ainda não submetido para revisão.</summary>
    Draft = 0,

    /// <summary>Submetido para revisão pelas equipas participantes.</summary>
    InReview = 1,

    /// <summary>Em negociação ativa entre as equipas.</summary>
    Negotiating = 2,

    /// <summary>Negociação aprovada — contrato aceite por todas as partes.</summary>
    Approved = 3,

    /// <summary>Negociação rejeitada — proposta recusada.</summary>
    Rejected = 4
}
