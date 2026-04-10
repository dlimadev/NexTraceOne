namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado do ciclo de vida de um executive briefing.
/// Transições válidas: Draft → Published → Archived.
/// </summary>
public enum BriefingStatus
{
    /// <summary>Rascunho — briefing gerado mas ainda não publicado.</summary>
    Draft = 1,

    /// <summary>Publicado — briefing disponível para consulta executiva.</summary>
    Published = 2,

    /// <summary>Arquivado — briefing retirado da vista ativa, preservado para histórico.</summary>
    Archived = 3
}
