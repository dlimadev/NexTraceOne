namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Estado de um pedido de aprovação de release (interno ou externo).
/// </summary>
public enum ApprovalRequestStatus
{
    /// <summary>Pedido criado, aguardando resposta do aprovador.</summary>
    Pending = 0,

    /// <summary>Release aprovada — promoção pode avançar.</summary>
    Approved = 1,

    /// <summary>Release rejeitada — promoção bloqueada.</summary>
    Rejected = 2,

    /// <summary>Token de callback expirou sem resposta.</summary>
    Expired = 3,

    /// <summary>Aprovação contornada por utilizador com papel de bypass.</summary>
    Bypassed = 4,
}
