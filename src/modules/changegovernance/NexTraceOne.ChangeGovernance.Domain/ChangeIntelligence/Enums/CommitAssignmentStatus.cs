namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Estado de associação de um commit ao ciclo de vida de releases.
/// Um commit transita de Unassigned/Candidate para Included quando vinculado
/// a uma release, ou para Excluded quando removido pelo PO/PM.
/// </summary>
public enum CommitAssignmentStatus
{
    /// <summary>Commit recebido sem correspondência com nenhuma release activa. Aguarda associação futura.</summary>
    Unassigned = 0,

    /// <summary>Commit em branch que corresponde a uma release em andamento. Candidato à inclusão automática.</summary>
    Candidate = 1,

    /// <summary>Commit incluído formalmente na release — irá a produção nesta release.</summary>
    Included = 2,

    /// <summary>Commit explicitamente excluído da release pelo PO/PM. Pode ser candidato para a próxima release.</summary>
    Excluded = 3,
}
