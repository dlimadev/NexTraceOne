namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Taxonomia de eventos de product analytics.
/// Cada tipo de evento responde a uma pergunta real sobre adoção, valor, fricção ou qualidade da experiência.
/// A taxonomia é estável e orientada a decisões de produto.
/// </summary>
public enum AnalyticsEventType
{
    /// <summary>Módulo visualizado pelo utilizador.</summary>
    ModuleViewed = 0,

    /// <summary>Entidade específica visualizada (serviço, contrato, incidente, etc.).</summary>
    EntityViewed = 1,

    /// <summary>Busca executada no produto.</summary>
    SearchExecuted = 2,

    /// <summary>Resultado de busca clicado pelo utilizador.</summary>
    SearchResultClicked = 3,

    /// <summary>Busca sem resultados — sinal de fricção ou gap de conteúdo.</summary>
    ZeroResultSearch = 4,

    /// <summary>Quick action acionada pelo utilizador.</summary>
    QuickActionTriggered = 5,

    /// <summary>Prompt submetido ao AI Assistant.</summary>
    AssistantPromptSubmitted = 6,

    /// <summary>Resposta do AI Assistant utilizada (clicada, copiada, aplicada).</summary>
    AssistantResponseUsed = 7,

    /// <summary>Rascunho de contrato criado no Contract Studio.</summary>
    ContractDraftCreated = 8,

    /// <summary>Contrato publicado oficialmente.</summary>
    ContractPublished = 9,

    /// <summary>Mudança visualizada no Change Intelligence.</summary>
    ChangeViewed = 10,

    /// <summary>Incidente investigado pelo utilizador.</summary>
    IncidentInvestigated = 11,

    /// <summary>Workflow de mitigação iniciado.</summary>
    MitigationWorkflowStarted = 12,

    /// <summary>Workflow de mitigação concluído com sucesso.</summary>
    MitigationWorkflowCompleted = 13,

    /// <summary>Evidence package exportado para auditoria.</summary>
    EvidencePackageExported = 14,

    /// <summary>Política de governança visualizada.</summary>
    PolicyViewed = 15,

    /// <summary>Executive overview consumido.</summary>
    ExecutiveOverviewViewed = 16,

    /// <summary>Runbook consultado.</summary>
    RunbookViewed = 17,

    /// <summary>Source of Truth consultado.</summary>
    SourceOfTruthQueried = 18,

    /// <summary>Relatório gerado ou visualizado.</summary>
    ReportGenerated = 19,

    /// <summary>Onboarding step concluído.</summary>
    OnboardingStepCompleted = 20,

    /// <summary>Jornada abandonada — sinal de fricção.</summary>
    JourneyAbandoned = 21,

    /// <summary>Empty state encontrado — pode indicar gap de configuração.</summary>
    EmptyStateEncountered = 22,

    /// <summary>Reliability dashboard consultado.</summary>
    ReliabilityDashboardViewed = 23,

    /// <summary>Automation workflow criado ou editado.</summary>
    AutomationWorkflowManaged = 24
}
