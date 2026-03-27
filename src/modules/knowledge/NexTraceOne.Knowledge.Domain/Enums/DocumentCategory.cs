namespace NexTraceOne.Knowledge.Domain.Enums;

/// <summary>
/// Categoria do documento de conhecimento no Knowledge Hub.
/// </summary>
public enum DocumentCategory
{
    /// <summary>Documentação geral.</summary>
    General,

    /// <summary>Runbook operacional.</summary>
    Runbook,

    /// <summary>Guia de resolução de problemas.</summary>
    Troubleshooting,

    /// <summary>Arquitectura e design.</summary>
    Architecture,

    /// <summary>Procedimento operacional standard.</summary>
    Procedure,

    /// <summary>Post-mortem ou análise de incidente.</summary>
    PostMortem,

    /// <summary>FAQ ou referência rápida.</summary>
    Reference
}
