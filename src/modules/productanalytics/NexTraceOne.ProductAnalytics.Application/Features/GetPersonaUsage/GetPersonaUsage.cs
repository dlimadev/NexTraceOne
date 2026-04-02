using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage;

/// <summary>
/// Retorna perfil de uso por persona.
/// Responde: quais personas usam quais capacidades? Qual a profundidade de uso?
/// Quais são os pontos de fricção e milestones atingidos por persona?
/// Consome dados reais do IAnalyticsEventRepository.
/// </summary>
public static class GetPersonaUsage
{
    /// <summary>Query para uso por persona com filtro opcional.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna o perfil de uso por persona a partir de dados reais.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        private static readonly HashSet<AnalyticsEventType> FrictionEventTypes =
        [
            AnalyticsEventType.ZeroResultSearch,
            AnalyticsEventType.EmptyStateEncountered,
            AnalyticsEventType.JourneyAbandoned
        ];

        private static readonly (ValueMilestoneType Milestone, AnalyticsEventType EventType)[] MilestoneMap =
        [
            (ValueMilestoneType.FirstSearchSuccess, AnalyticsEventType.SearchResultClicked),
            (ValueMilestoneType.FirstServiceLookup, AnalyticsEventType.EntityViewed),
            (ValueMilestoneType.FirstContractView, AnalyticsEventType.EntityViewed),
            (ValueMilestoneType.FirstContractDraftCreated, AnalyticsEventType.ContractDraftCreated),
            (ValueMilestoneType.FirstContractPublished, AnalyticsEventType.ContractPublished),
            (ValueMilestoneType.FirstAiUsefulInteraction, AnalyticsEventType.AssistantResponseUsed),
            (ValueMilestoneType.FirstIncidentInvestigation, AnalyticsEventType.IncidentInvestigated),
            (ValueMilestoneType.FirstMitigationCompleted, AnalyticsEventType.MitigationWorkflowCompleted),
            (ValueMilestoneType.FirstExecutiveOverviewConsumed, AnalyticsEventType.ExecutiveOverviewViewed),
            (ValueMilestoneType.FirstRunbookConsulted, AnalyticsEventType.RunbookViewed),
            (ValueMilestoneType.FirstSourceOfTruthUsed, AnalyticsEventType.SourceOfTruthQueried),
            (ValueMilestoneType.FirstEvidenceExported, AnalyticsEventType.EvidencePackageExported),
            (ValueMilestoneType.FirstReportGenerated, AnalyticsEventType.ReportGenerated),
            (ValueMilestoneType.FirstReliabilityViewed, AnalyticsEventType.ReliabilityDashboardViewed),
            (ValueMilestoneType.FirstAutomationCreated, AnalyticsEventType.OnboardingStepCompleted)
        ];

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range);

            var personas = await repository.GetPersonaBreakdownAsync(
                request.TeamId, domainId: null, from, to, cancellationToken);

            if (personas.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    Profiles: Array.Empty<PersonaUsageProfileDto>(),
                    TotalPersonas: 0,
                    MostActivePersona: string.Empty,
                    DeepestAdoptionPersona: string.Empty,
                    PeriodLabel: periodLabel));
            }

            if (!string.IsNullOrWhiteSpace(request.Persona))
            {
                personas = personas
                    .Where(p => p.Persona.Equals(request.Persona, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var maxActionsPerUser = personas
                .Where(p => p.UniqueUsers > 0)
                .Select(p => p.EventCount / (decimal)p.UniqueUsers)
                .DefaultIfEmpty(1m)
                .Max();

            if (maxActionsPerUser <= 0) maxActionsPerUser = 1m;

            var profiles = new List<PersonaUsageProfileDto>();

            foreach (var persona in personas)
            {
                var topModules = await repository.GetTopModulesAsync(
                    persona: persona.Persona, teamId: request.TeamId, domainId: null,
                    from, to, top: 4, cancellationToken);

                var topEventTypes = await repository.GetTopEventTypesAsync(
                    persona: persona.Persona, from, to, top: 6, cancellationToken);

                var distinctEventTypes = await repository.GetDistinctEventTypesAsync(
                    persona: persona.Persona, from, to, cancellationToken);

                var actionsPerUser = persona.UniqueUsers > 0
                    ? persona.EventCount / (decimal)persona.UniqueUsers
                    : 0m;

                var adoptionDepth = Math.Round(Math.Min(100m, (actionsPerUser / maxActionsPerUser) * 100m), 1);

                var frictionPoints = topEventTypes
                    .Where(e => FrictionEventTypes.Contains(e.EventType))
                    .Select(e => MapFrictionLabel(e.EventType))
                    .ToArray();

                var distinctSet = distinctEventTypes.ToHashSet();
                var milestonesReached = MilestoneMap
                    .Where(m => distinctSet.Contains(m.EventType))
                    .Select(m => m.Milestone)
                    .Distinct()
                    .ToArray();

                var moduleDtos = topModules
                    .Select(m => new PersonaModuleDto(
                        m.Module,
                        persona.UniqueUsers > 0
                            ? (int)Math.Round((m.UniqueUsers / (decimal)persona.UniqueUsers) * 100m)
                            : 0,
                        m.EventCount))
                    .ToArray();

                var topActions = topEventTypes
                    .Where(e => !FrictionEventTypes.Contains(e.EventType))
                    .Take(4)
                    .Select(e => MapEventTypeLabel(e.EventType))
                    .ToArray();

                profiles.Add(new PersonaUsageProfileDto(
                    Persona: persona.Persona,
                    ActiveUsers: persona.UniqueUsers,
                    TotalActions: persona.EventCount,
                    TopModules: moduleDtos,
                    TopActions: topActions,
                    AdoptionDepth: adoptionDepth,
                    CommonFrictionPoints: frictionPoints,
                    MilestonesReached: milestonesReached));
            }

            var mostActive = profiles.OrderByDescending(p => p.TotalActions).FirstOrDefault()?.Persona ?? string.Empty;
            var deepest = profiles.OrderByDescending(p => p.AdoptionDepth).FirstOrDefault()?.Persona ?? string.Empty;

            return Result<Response>.Success(new Response(
                Profiles: profiles,
                TotalPersonas: profiles.Count,
                MostActivePersona: mostActive,
                DeepestAdoptionPersona: deepest,
                PeriodLabel: periodLabel));
        }

        private static (DateTimeOffset From, DateTimeOffset To, string Label) ResolveRange(DateTimeOffset utcNow, string? range)
        {
            var label = string.IsNullOrWhiteSpace(range) ? "last_30d" : range;
            var days = label switch
            {
                "last_7d" => 7,
                "last_1d" => 1,
                "last_90d" => 90,
                _ => 30
            };
            return (utcNow.AddDays(-days), utcNow, label);
        }

        private static string MapFrictionLabel(AnalyticsEventType eventType) => eventType switch
        {
            AnalyticsEventType.ZeroResultSearch => "zero_result_search",
            AnalyticsEventType.EmptyStateEncountered => "empty_state_encountered",
            AnalyticsEventType.JourneyAbandoned => "journey_abandoned",
            _ => eventType.ToString()
        };

        private static string MapEventTypeLabel(AnalyticsEventType eventType) => eventType switch
        {
            AnalyticsEventType.SearchExecuted => "search_executed",
            AnalyticsEventType.SearchResultClicked => "search_result_clicked",
            AnalyticsEventType.EntityViewed => "entity_viewed",
            AnalyticsEventType.ContractDraftCreated => "contract_draft_created",
            AnalyticsEventType.ContractPublished => "contract_published",
            AnalyticsEventType.AssistantPromptSubmitted => "assistant_prompt_submitted",
            AnalyticsEventType.AssistantResponseUsed => "assistant_response_used",
            AnalyticsEventType.ChangeViewed => "change_viewed",
            AnalyticsEventType.IncidentInvestigated => "incident_investigated",
            AnalyticsEventType.MitigationWorkflowStarted => "mitigation_started",
            AnalyticsEventType.MitigationWorkflowCompleted => "mitigation_completed",
            AnalyticsEventType.ExecutiveOverviewViewed => "executive_overview_viewed",
            AnalyticsEventType.ReportGenerated => "report_generated",
            AnalyticsEventType.SourceOfTruthQueried => "source_of_truth_queried",
            _ => eventType.ToString()
        };
    }

    /// <summary>Resposta com perfis de uso por persona.</summary>
    public sealed record Response(
        IReadOnlyList<PersonaUsageProfileDto> Profiles,
        int TotalPersonas,
        string MostActivePersona,
        string DeepestAdoptionPersona,
        string PeriodLabel);

    /// <summary>Perfil de uso de uma persona específica.</summary>
    public sealed record PersonaUsageProfileDto(
        string Persona,
        int ActiveUsers,
        long TotalActions,
        IReadOnlyList<PersonaModuleDto> TopModules,
        IReadOnlyList<string> TopActions,
        decimal AdoptionDepth,
        IReadOnlyList<string> CommonFrictionPoints,
        IReadOnlyList<ValueMilestoneType> MilestonesReached);

    /// <summary>Uso de módulo por persona.</summary>
    public sealed record PersonaModuleDto(
        ProductModule Module,
        int AdoptionPercent,
        long ActionCount);
}
