namespace NexTraceOne.ProductAnalytics.Domain.Enums;

/// <summary>
/// Marcos de valor do produto.
/// Cada milestone corresponde a um momento em que o utilizador atinge valor real no NexTraceOne.
/// Usado para medir time-to-first-value e time-to-core-value.
/// </summary>
public enum ValueMilestoneType
{
    /// <summary>Primeira busca com resultado útil.</summary>
    FirstSearchSuccess = 0,

    /// <summary>Primeira consulta a um serviço no catálogo.</summary>
    FirstServiceLookup = 1,

    /// <summary>Primeiro contrato visualizado.</summary>
    FirstContractView = 2,

    /// <summary>Primeiro rascunho de contrato criado.</summary>
    FirstContractDraftCreated = 3,

    /// <summary>Primeiro contrato publicado.</summary>
    FirstContractPublished = 4,

    /// <summary>Primeira interação útil com o AI Assistant.</summary>
    FirstAiUsefulInteraction = 5,

    /// <summary>Primeira investigação de incidente.</summary>
    FirstIncidentInvestigation = 6,

    /// <summary>Primeiro workflow de mitigação concluído.</summary>
    FirstMitigationCompleted = 7,

    /// <summary>Primeira consulta ao executive overview.</summary>
    FirstExecutiveOverviewConsumed = 8,

    /// <summary>Primeiro runbook consultado.</summary>
    FirstRunbookConsulted = 9,

    /// <summary>Primeiro source of truth consultado para operação real.</summary>
    FirstSourceOfTruthUsed = 10,

    /// <summary>Primeiro evidence package exportado.</summary>
    FirstEvidenceExported = 11,

    /// <summary>Primeiro relatório gerado.</summary>
    FirstReportGenerated = 12,

    /// <summary>Primeira reliability view consultada.</summary>
    FirstReliabilityViewed = 13,

    /// <summary>Primeiro automation workflow criado.</summary>
    FirstAutomationCreated = 14
}
