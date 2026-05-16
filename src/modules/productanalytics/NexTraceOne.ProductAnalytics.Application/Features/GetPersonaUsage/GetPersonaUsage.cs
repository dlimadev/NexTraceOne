using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.ConfigurationKeys;
using NexTraceOne.ProductAnalytics.Application.Constants;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage;

/// <summary>
/// Retorna perfil de uso por persona.
/// Responde: quais personas usam quais capacidades? Qual a profundidade de uso?
/// Quais são os pontos de fricção e milestones atingidos por persona?
/// Consome dados reais do IAnalyticsEventRepository.
/// </summary>
public static class GetPersonaUsage
{
    /// <summary>Query para uso por persona com filtro opcional e paginação.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna o perfil de uso por persona a partir de dados reais.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxRangeCfg = await configService.ResolveEffectiveValueAsync(AnalyticsConfigKeys.MaxRangeDays, ConfigurationScope.System, null, cancellationToken);
            var maxRangeDays = int.TryParse(maxRangeCfg?.EffectiveValue, out var mrd) ? mrd : AnalyticsConstants.MaxRangeDays;

            var (from, to, periodLabel) = AnalyticsQueryHelper.ResolveRange(clock.UtcNow, request.Range, maxRangeDays);

            var personas = await repository.GetPersonaBreakdownAsync(
                request.TeamId, domainId: null, from, to, cancellationToken);

            if (personas.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    Profiles: Array.Empty<PersonaUsageProfileDto>(),
                    TotalPersonas: 0,
                    MostActivePersona: string.Empty,
                    DeepestAdoptionPersona: string.Empty,
                    PeriodLabel: periodLabel,
                    Page: 1,
                    PageSize: request.PageSize,
                    TotalCount: 0,
                    TotalPages: 0));
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
                    .Where(e => AnalyticsConstants.FrictionEventTypes.Contains(e.EventType))
                    .Select(e => MapFrictionLabel(e.EventType))
                    .ToArray();

                var distinctSet = distinctEventTypes.ToHashSet();
                var milestoneMap = AnalyticsConstants.MilestoneDefs.Select(d => (d.Type, d.EventType)).ToArray();
                var milestonesReached = milestoneMap
                    .Where(m => distinctSet.Contains(m.EventType))
                    .Select(m => m.Type)
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
                    .Where(e => !AnalyticsConstants.FrictionEventTypes.Contains(e.EventType))
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

            var totalCount = profiles.Count;
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
            var pagedProfiles = profiles.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Result<Response>.Success(new Response(
                Profiles: pagedProfiles,
                TotalPersonas: totalCount,
                MostActivePersona: mostActive,
                DeepestAdoptionPersona: deepest,
                PeriodLabel: periodLabel,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: Math.Max(1, totalPages)));
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

    /// <summary>Resposta com perfis de uso por persona e metadados de paginação.</summary>
    public sealed record Response(
        IReadOnlyList<PersonaUsageProfileDto> Profiles,
        int TotalPersonas,
        string MostActivePersona,
        string DeepestAdoptionPersona,
        string PeriodLabel,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

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
