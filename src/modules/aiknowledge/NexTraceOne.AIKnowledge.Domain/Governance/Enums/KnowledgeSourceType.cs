namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de fonte de conhecimento utilizada para grounding/contexto das consultas de IA.
/// Cada tipo representa um domínio do NexTraceOne como Source of Truth.
/// </summary>
public enum KnowledgeSourceType
{
    /// <summary>Informações de serviços do Service Catalog.</summary>
    Service,

    /// <summary>Contratos de API, eventos ou serviços.</summary>
    Contract,

    /// <summary>Dados e histórico de incidentes.</summary>
    Incident,

    /// <summary>Informações de mudanças e releases.</summary>
    Change,

    /// <summary>Runbooks operacionais.</summary>
    Runbook,

    /// <summary>Documentação técnica e operacional.</summary>
    Documentation,

    /// <summary>Resumos de telemetria e métricas contextualizadas.</summary>
    TelemetrySummary,

    /// <summary>Vista consolidada da Source of Truth.</summary>
    SourceOfTruth
}
