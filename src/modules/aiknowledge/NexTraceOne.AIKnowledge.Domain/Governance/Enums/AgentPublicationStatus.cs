namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Estado de publicação do agent — controla o ciclo de vida de disponibilidade.
/// Draft: em construção. PendingReview: aguarda aprovação. Active: disponível.
/// Published: disponível para outros. Archived: desactivado. Blocked: bloqueado por governança.
/// </summary>
public enum AgentPublicationStatus
{
    Draft = 0,
    PendingReview = 1,
    Active = 2,
    Published = 3,
    Archived = 4,
    Blocked = 5,
}
