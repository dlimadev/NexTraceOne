namespace NexTraceOne.ProductAnalytics.Domain.Enums;

/// <summary>
/// Módulos do produto NexTraceOne para medição de adoção e uso.
/// Cada módulo corresponde a uma área funcional principal do produto.
/// </summary>
public enum ProductModule
{
    /// <summary>Dashboard principal.</summary>
    Dashboard = 0,

    /// <summary>Catálogo de serviços e ownership.</summary>
    ServiceCatalog = 1,

    /// <summary>Source of Truth — consulta oficial de contratos, serviços e conhecimento.</summary>
    SourceOfTruth = 2,

    /// <summary>Contract Studio — criação, edição e governança de contratos.</summary>
    ContractStudio = 3,

    /// <summary>Change Intelligence — confiança em mudanças de produção.</summary>
    ChangeIntelligence = 4,

    /// <summary>Incidentes e mitigação operacional.</summary>
    Incidents = 5,

    /// <summary>Reliability — confiabilidade de serviço por equipa.</summary>
    Reliability = 6,

    /// <summary>Runbooks operacionais.</summary>
    Runbooks = 7,

    /// <summary>AI Assistant — assistente de IA governado.</summary>
    AiAssistant = 8,

    /// <summary>Governance — relatórios, risco, compliance, políticas.</summary>
    Governance = 9,

    /// <summary>Executive Views — visões executivas consolidadas.</summary>
    ExecutiveViews = 10,

    /// <summary>FinOps — otimização contextual de custos.</summary>
    FinOps = 11,

    /// <summary>Integration Hub — conectores e ingestão.</summary>
    IntegrationHub = 12,

    /// <summary>Developer Portal — portal de APIs para consumidores.</summary>
    DeveloperPortal = 13,

    /// <summary>Admin — gestão de utilizadores, acessos e configurações.</summary>
    Admin = 14,

    /// <summary>Automation — workflows de automação operacional.</summary>
    Automation = 15,

    /// <summary>Busca global e command palette.</summary>
    Search = 16
}
