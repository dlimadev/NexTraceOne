namespace NexTraceOne.Knowledge.Domain.Enums;

/// <summary>
/// Categoria do documento de conhecimento no Knowledge Hub (Operational Knowledge).
/// Expandido conforme PARAMETERIZATION-MODULE-PROPOSAL Phase 3 para suportar
/// classificação mais rica de conhecimento operacional e técnico.
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
    Reference,

    /// <summary>Documentação de API (REST, SOAP, eventos).</summary>
    ApiDocumentation,

    /// <summary>Registo de alterações (changelog) de serviço ou contrato.</summary>
    ChangeLog,

    /// <summary>Evidência de conformidade (compliance evidence).</summary>
    ComplianceEvidence,

    /// <summary>Registo de decisão técnica ou arquitetural (ADR).</summary>
    DecisionRecord,

    /// <summary>Análise detalhada de incidente com causa raiz e mitigação.</summary>
    IncidentAnalysis,

    /// <summary>Playbook operacional com passos de resposta.</summary>
    OperationalPlaybook
}
