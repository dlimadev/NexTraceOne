namespace NexTraceOne.Knowledge.Domain.Enums;

/// <summary>
/// Tipo de relação entre um objecto de conhecimento e outra entidade do sistema.
/// Prepara a ligação entre Knowledge Hub e serviços, contratos, mudanças e incidentes.
/// </summary>
public enum RelationType
{
    /// <summary>Documento relacionado com um serviço.</summary>
    Service,

    /// <summary>Documento relacionado com um contrato API.</summary>
    Contract,

    /// <summary>Documento relacionado com uma mudança/release.</summary>
    Change,

    /// <summary>Documento relacionado com um incidente.</summary>
    Incident,

    /// <summary>Documento relacionado com outro documento de conhecimento.</summary>
    KnowledgeDocument,

    /// <summary>Documento relacionado com um runbook.</summary>
    Runbook,

    /// <summary>Relação genérica/custom.</summary>
    Other
}
