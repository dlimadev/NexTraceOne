namespace NexTraceOne.Knowledge.Domain.Enums;

/// <summary>
/// Estado do ciclo de vida de um KnowledgeDocument.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Rascunho — ainda não publicado.</summary>
    Draft,

    /// <summary>Publicado e visível para consumo.</summary>
    Published,

    /// <summary>Arquivado — mantido para referência mas não é mostrado por defeito.</summary>
    Archived,

    /// <summary>Obsoleto — substituído por versão mais recente.</summary>
    Deprecated
}
