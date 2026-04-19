using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Constants;

/// <summary>
/// Constantes e mapeamentos centralizados do módulo ProductAnalytics.
/// Elimina duplicação entre handlers e garante consistência nas definições de milestones,
/// jornadas e eventos.
/// </summary>
public static class AnalyticsConstants
{
    /// <summary>
    /// Mapeamento canónico de milestone de valor para o tipo de evento que o confirma.
    /// Partilhado por GetValueMilestones, GetPersonaUsage e GetJourneys.
    /// NOTA: FirstAutomationCreated usa AutomationWorkflowManaged (não OnboardingStepCompleted).
    /// </summary>
    public static readonly IReadOnlyList<(ValueMilestoneType Type, string Name, AnalyticsEventType EventType)> MilestoneDefs =
    [
        (ValueMilestoneType.FirstSearchSuccess,            "First Search Success",         AnalyticsEventType.SearchResultClicked),
        (ValueMilestoneType.FirstServiceLookup,            "First Service Lookup",         AnalyticsEventType.EntityViewed),
        (ValueMilestoneType.FirstContractView,             "First Contract View",          AnalyticsEventType.EntityViewed),
        (ValueMilestoneType.FirstContractDraftCreated,     "First Contract Draft",         AnalyticsEventType.ContractDraftCreated),
        (ValueMilestoneType.FirstContractPublished,        "First Contract Published",     AnalyticsEventType.ContractPublished),
        (ValueMilestoneType.FirstAiUsefulInteraction,      "First AI Useful Interaction",  AnalyticsEventType.AssistantResponseUsed),
        (ValueMilestoneType.FirstIncidentInvestigation,    "First Incident Investigation", AnalyticsEventType.IncidentInvestigated),
        (ValueMilestoneType.FirstMitigationCompleted,      "First Mitigation Completed",   AnalyticsEventType.MitigationWorkflowCompleted),
        (ValueMilestoneType.FirstExecutiveOverviewConsumed,"First Executive Overview",     AnalyticsEventType.ExecutiveOverviewViewed),
        (ValueMilestoneType.FirstRunbookConsulted,         "First Runbook Consulted",      AnalyticsEventType.RunbookViewed),
        (ValueMilestoneType.FirstSourceOfTruthUsed,        "First Source of Truth Used",   AnalyticsEventType.SourceOfTruthQueried),
        (ValueMilestoneType.FirstEvidenceExported,         "First Evidence Exported",      AnalyticsEventType.EvidencePackageExported),
        (ValueMilestoneType.FirstReportGenerated,          "First Report Generated",       AnalyticsEventType.ReportGenerated),
        (ValueMilestoneType.FirstReliabilityViewed,        "First Reliability Viewed",     AnalyticsEventType.ReliabilityDashboardViewed),
        (ValueMilestoneType.FirstAutomationCreated,        "First Automation Created",     AnalyticsEventType.AutomationWorkflowManaged)
    ];

    /// <summary>Eventos que representam valor entregue ao utilizador.</summary>
    public static readonly IReadOnlyList<AnalyticsEventType> ValueEventTypes =
    [
        AnalyticsEventType.ContractPublished,
        AnalyticsEventType.AutomationWorkflowManaged,
        AnalyticsEventType.AssistantResponseUsed,
        AnalyticsEventType.MitigationWorkflowCompleted
    ];

    /// <summary>Eventos que representam valor core (alto impacto).</summary>
    public static readonly IReadOnlyList<AnalyticsEventType> CoreValueEventTypes =
    [
        AnalyticsEventType.ContractPublished,
        AnalyticsEventType.MitigationWorkflowCompleted
    ];

    /// <summary>Eventos que representam fricção do utilizador.</summary>
    public static readonly IReadOnlyList<AnalyticsEventType> FrictionEventTypes =
    [
        AnalyticsEventType.ZeroResultSearch,
        AnalyticsEventType.EmptyStateEncountered,
        AnalyticsEventType.JourneyAbandoned
    ];

    /// <summary>Número de módulos top a retornar por defeito.</summary>
    public const int TopModulesLimit = 6;

    /// <summary>Número de features top a retornar por defeito.</summary>
    public const int TopFeaturesLimit = 5;

    /// <summary>Threshold de tendência por defeito (5% de variação).</summary>
    public const decimal TrendThreshold = 0.05m;

    /// <summary>Range de período por defeito.</summary>
    public const string DefaultRange = "last_30d";

    /// <summary>Número máximo de dias permitidos numa janela de consulta.</summary>
    public const int MaxRangeDays = 180;
}
