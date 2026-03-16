namespace NexTraceOne.AiGovernance.Domain.Enums;

/// <summary>
/// Classificação do caso de uso da consulta de IA.
/// Determina modelo preferencial, fontes de contexto e profundidade da resposta.
/// </summary>
public enum AIUseCaseType
{
    /// <summary>Consulta sobre um serviço do catálogo.</summary>
    ServiceLookup,

    /// <summary>Explicação de contrato (API, SOAP, evento, etc.).</summary>
    ContractExplanation,

    /// <summary>Geração assistida de contrato — requer modelo mais capaz.</summary>
    ContractGeneration,

    /// <summary>Explicação de incidente com correlação operacional.</summary>
    IncidentExplanation,

    /// <summary>Orientação de mitigação baseada em runbooks e histórico.</summary>
    MitigationGuidance,

    /// <summary>Resumo executivo — conciso e custo-eficiente.</summary>
    ExecutiveSummary,

    /// <summary>Explicação de risco ou conformidade.</summary>
    RiskComplianceExplanation,

    /// <summary>Explicação de FinOps contextual.</summary>
    FinOpsExplanation,

    /// <summary>Raciocínio sobre dependências entre serviços.</summary>
    DependencyReasoning,

    /// <summary>Análise de mudança com blast radius e confiança.</summary>
    ChangeAnalysis,

    /// <summary>Consulta geral sem classificação específica.</summary>
    General
}
