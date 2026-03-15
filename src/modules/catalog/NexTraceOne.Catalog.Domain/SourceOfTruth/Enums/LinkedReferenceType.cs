namespace NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

/// <summary>
/// Tipo de referência vinculada a um ativo no Source of Truth.
/// Permite classificar documentação, runbooks, notas operacionais e links contextuais.
/// </summary>
public enum LinkedReferenceType
{
    /// <summary>Documentação oficial do serviço ou contrato.</summary>
    Documentation = 0,

    /// <summary>Runbook operacional para mitigação ou operação.</summary>
    Runbook = 1,

    /// <summary>Nota operacional ou contexto adicional.</summary>
    OperationalNote = 2,

    /// <summary>Link externo relevante (wiki, confluence, etc.).</summary>
    ExternalLink = 3,

    /// <summary>Changelog ou histórico de mudanças resumido.</summary>
    Changelog = 4,

    /// <summary>Referência a tópico Kafka ou stream de eventos.</summary>
    EventTopic = 5,

    /// <summary>Referência a API relacionada ou dependência.</summary>
    RelatedApi = 6,

    /// <summary>Referência a incidente operacional relevante.</summary>
    RelatedIncident = 7
}
