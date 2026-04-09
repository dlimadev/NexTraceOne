namespace NexTraceOne.Knowledge.Domain.Enums;

/// <summary>
/// Estado do snapshot do knowledge graph no ciclo de vida.
/// Generated → Reviewed (após análise humana) ou Stale (quando substituído).
/// </summary>
public enum KnowledgeGraphSnapshotStatus
{
    /// <summary>Snapshot recém-gerado, aguarda revisão.</summary>
    Generated,

    /// <summary>Snapshot revisado por um utilizador.</summary>
    Reviewed,

    /// <summary>Snapshot obsoleto, substituído por um mais recente.</summary>
    Stale
}
