namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado do ciclo de vida de uma equipa dentro da plataforma de governança.
/// Controla a visibilidade e as operações permitidas sobre a equipa.
/// </summary>
public enum TeamStatus
{
    /// <summary>Equipa ativa e operacional.</summary>
    Active = 0,

    /// <summary>Equipa temporariamente desativada, sem permissões operacionais.</summary>
    Inactive = 1,

    /// <summary>Equipa arquivada, mantida apenas para histórico e auditoria.</summary>
    Archived = 2
}
