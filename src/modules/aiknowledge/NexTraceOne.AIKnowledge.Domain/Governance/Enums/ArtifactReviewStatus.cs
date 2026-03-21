namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Estado de revisão de um artefacto gerado por agent.
/// Pending: aguarda revisão. Approved: aprovado. Rejected: rejeitado. Superseded: substituído por nova versão.
/// </summary>
public enum ArtifactReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Superseded = 3,
}
