using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.ConfigurationKeys;
using NexTraceOne.ProductAnalytics.Application.Constants;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using System.Text.Json;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetJourneys;

/// <summary>
/// Retorna jornadas e funis do produto com métricas de conclusão.
/// Responde: quais jornadas chegam até valor real? Onde há abandono?
/// Qual é o tempo médio por jornada? Onde estão os pontos de drop-off?
/// Consome dados reais do IAnalyticsEventRepository — computação de funil
/// baseada em presença de tipos de evento por sessão.
/// As definições de jornada são carregadas do banco (IJourneyDefinitionRepository)
/// com fallback para as definições estáticas da plataforma.
/// </summary>
public static class GetJourneys
{
    /// <summary>Query para jornadas e funis do produto.</summary>
    public sealed record Query(
        string? JourneyId,
        string? Persona,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna métricas de jornadas a partir de dados reais.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock,
        IConfigurationResolutionService configService,
        IJourneyDefinitionRepository? journeyDefinitionRepository = null,
        ICurrentTenant? tenant = null) : IQueryHandler<Query, Response>
    {
        /// <summary>
        /// Definições estáticas de fallback — usadas quando não existem definições no banco.
        /// Mantidas para retrocompatibilidade e para novos tenants sem configuração.
        /// </summary>
        private static readonly JourneyDefinition[] StaticJourneyDefinitions =
        [
            new("search_to_entity", "Search to Entity View",
            [
                new("search_executed", "Search Executed", AnalyticsEventType.SearchExecuted),
                new("results_displayed", "Results Displayed", AnalyticsEventType.SearchResultClicked),
                new("entity_viewed", "Entity Viewed", AnalyticsEventType.EntityViewed)
            ]),
            new("ai_prompt_to_action", "AI Prompt to Useful Action",
            [
                new("assistant_opened", "Assistant Opened", AnalyticsEventType.AssistantPromptSubmitted),
                new("response_received", "Response Received", AnalyticsEventType.AssistantResponseUsed),
            ]),
            new("contract_draft_to_publish", "Contract Draft to Publication",
            [
                new("draft_created", "Draft Created", AnalyticsEventType.ContractDraftCreated),
                new("contract_published", "Contract Published", AnalyticsEventType.ContractPublished)
            ]),
            new("incident_to_mitigation", "Incident to Mitigation Completion",
            [
                new("incident_opened", "Incident Opened", AnalyticsEventType.IncidentInvestigated),
                new("mitigation_started", "Mitigation Started", AnalyticsEventType.MitigationWorkflowStarted),
                new("mitigation_completed", "Mitigation Completed", AnalyticsEventType.MitigationWorkflowCompleted)
            ]),
            new("onboarding_to_first_action", "Onboarding to First Meaningful Action",
            [
                new("first_login", "First Login", AnalyticsEventType.ModuleViewed),
                new("first_search", "First Search", AnalyticsEventType.SearchExecuted),
                new("first_meaningful_action", "First Meaningful Action", AnalyticsEventType.OnboardingStepCompleted)
            ])
        ];

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxRangeCfg = await configService.ResolveEffectiveValueAsync(AnalyticsConfigKeys.MaxRangeDays, ConfigurationScope.System, null, cancellationToken);
            var maxRangeDays = int.TryParse(maxRangeCfg?.EffectiveValue, out var mrd) ? mrd : AnalyticsConstants.MaxRangeDays;

            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range, maxRangeDays);

            // Load journey definitions from DB if available, otherwise use static fallback
            JourneyDefinition[] journeyDefinitions;
            if (journeyDefinitionRepository is not null && tenant is not null)
            {
                var dbDefs = await journeyDefinitionRepository.ListActiveAsync(tenant.Id, cancellationToken);
                journeyDefinitions = dbDefs.Count > 0
                    ? dbDefs.Select(ParseDbDefinition).Where(d => d is not null).Cast<JourneyDefinition>().ToArray()
                    : StaticJourneyDefinitions;
            }
            else
            {
                journeyDefinitions = StaticJourneyDefinitions;
            }

            var allEventTypes = journeyDefinitions
                .SelectMany(j => j.Steps.Select(s => s.EventType))
                .Distinct()
                .ToArray();

            var sessionEventTypes = await repository.GetSessionEventTypesAsync(
                allEventTypes, request.Persona, from, to, cancellationToken);

            if (sessionEventTypes.Count == 0)
            {
                var skeletonJourneys = journeyDefinitions
                    .Where(def => string.IsNullOrWhiteSpace(request.JourneyId) ||
                                  def.JourneyId.Equals(request.JourneyId, StringComparison.OrdinalIgnoreCase))
                    .Select(def =>
                    {
                        var steps = def.Steps.Select((s, idx) =>
                            new JourneyStepDto(s.StepId, s.StepName, 0m, idx)).ToArray();
                        return new JourneyDto(def.JourneyId, def.JourneyName, steps,
                            0m, 0m, JourneyStatus.Started, string.Empty);
                    })
                    .ToArray();

                return Result<Response>.Success(new Response(
                    Journeys: skeletonJourneys,
                    AverageCompletionRate: 0m,
                    MostCompletedJourney: string.Empty,
                    HighestDropOffJourney: string.Empty,
                    PeriodLabel: periodLabel));
            }

            var sessionMap = sessionEventTypes
                .GroupBy(e => e.SessionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(x => x.EventType, x => x.FirstOccurrence));

            var journeys = new List<JourneyDto>();

            foreach (var def in journeyDefinitions)
            {
                if (!string.IsNullOrWhiteSpace(request.JourneyId) &&
                    !def.JourneyId.Equals(request.JourneyId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var firstStepType = def.Steps[0].EventType;
                var sessionsWithStart = sessionMap
                    .Where(s => s.Value.ContainsKey(firstStepType))
                    .ToList();

                var totalStartSessions = sessionsWithStart.Count;
                if (totalStartSessions == 0)
                {
                    var emptySteps = def.Steps.Select((s, idx) =>
                        new JourneyStepDto(s.StepId, s.StepName, idx == 0 ? 100m : 0m, idx)).ToArray();

                    journeys.Add(new JourneyDto(
                        def.JourneyId, def.JourneyName, emptySteps,
                        0m, 0m, JourneyStatus.Started, string.Empty));
                    continue;
                }

                var steps = new List<JourneyStepDto>();
                var biggestDropOff = string.Empty;
                var biggestDropValue = 0m;
                var totalDurationMinutes = new List<decimal>();

                for (var i = 0; i < def.Steps.Length; i++)
                {
                    var step = def.Steps[i];
                    var sessionsReachingStep = sessionsWithStart
                        .Count(s => s.Value.ContainsKey(step.EventType));

                    var completionPercent = Math.Round(
                        (sessionsReachingStep / (decimal)totalStartSessions) * 100m, 1);

                    steps.Add(new JourneyStepDto(step.StepId, step.StepName, completionPercent, i));

                    if (i > 0)
                    {
                        var drop = steps[i - 1].CompletionPercent - completionPercent;
                        if (drop > biggestDropValue)
                        {
                            biggestDropValue = drop;
                            biggestDropOff = $"{def.Steps[i - 1].StepName} → {step.StepName}";
                        }
                    }
                }

                var lastStep = def.Steps[^1];
                foreach (var session in sessionsWithStart)
                {
                    if (session.Value.TryGetValue(firstStepType, out var startTime) &&
                        session.Value.TryGetValue(lastStep.EventType, out var endTime))
                    {
                        var duration = (decimal)(endTime - startTime).TotalMinutes;
                        if (duration >= 0) totalDurationMinutes.Add(duration);
                    }
                }

                var avgDuration = totalDurationMinutes.Count > 0
                    ? Math.Round(totalDurationMinutes.Average(), 1)
                    : 0m;

                var completionRate = steps.Count > 0 ? steps[^1].CompletionPercent : 0m;
                var status = completionRate >= 50 ? JourneyStatus.Completed : JourneyStatus.InProgress;

                journeys.Add(new JourneyDto(
                    def.JourneyId, def.JourneyName, steps,
                    completionRate, avgDuration, status, biggestDropOff));
            }

            var avgCompletionRate = journeys.Count > 0
                ? Math.Round(journeys.Average(j => j.CompletionRate), 1)
                : 0m;

            var mostCompleted = journeys.OrderByDescending(j => j.CompletionRate).FirstOrDefault()?.JourneyId ?? string.Empty;
            var highestDropOff = journeys.OrderBy(j => j.CompletionRate).FirstOrDefault()?.JourneyId ?? string.Empty;

            return Result<Response>.Success(new Response(
                Journeys: journeys,
                AverageCompletionRate: avgCompletionRate,
                MostCompletedJourney: mostCompleted,
                HighestDropOffJourney: highestDropOff,
                PeriodLabel: periodLabel));
        }

        /// <summary>
        /// Parseia uma definição do banco para o formato interno.
        /// Returns null se o JSON dos steps não for válido — evita crashes em definições corrompidas.
        /// </summary>
        private static JourneyDefinition? ParseDbDefinition(NexTraceOne.ProductAnalytics.Domain.Entities.JourneyDefinition dbDef)
        {
            try
            {
                var steps = JsonSerializer.Deserialize<JourneyStepJson[]>(dbDef.StepsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (steps is null || steps.Length == 0) return null;

                var stepDefs = steps
                    .Where(s => !string.IsNullOrWhiteSpace(s.StepId)
                             && !string.IsNullOrWhiteSpace(s.StepName)
                             && Enum.TryParse<AnalyticsEventType>(s.EventType, out _))
                    .Select(s => new JourneyStepDefinition(
                        s.StepId!,
                        s.StepName!,
                        Enum.Parse<AnalyticsEventType>(s.EventType!)))
                    .ToArray();

                return stepDefs.Length > 0
                    ? new JourneyDefinition(dbDef.Key, dbDef.Name, stepDefs)
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static (DateTimeOffset From, DateTimeOffset To, string Label) ResolveRange(DateTimeOffset utcNow, string? range, int maxDays = AnalyticsConstants.MaxRangeDays)
        {
            var label = string.IsNullOrWhiteSpace(range) ? "last_30d" : range;
            var days = label switch
            {
                "last_7d" => 7,
                "last_1d" => 1,
                "last_90d" => 90,
                _ => 30
            };
            if (days > maxDays) days = maxDays;
            return (utcNow.AddDays(-days), utcNow, label);
        }
    }

    private sealed record JourneyDefinition(string JourneyId, string JourneyName, JourneyStepDefinition[] Steps);
    private sealed record JourneyStepDefinition(string StepId, string StepName, AnalyticsEventType EventType);

    /// <summary>DTO intermediário para desserialização do JSON de steps.</summary>
    private sealed class JourneyStepJson
    {
        public string? StepId { get; set; }
        public string? StepName { get; set; }
        public string? EventType { get; set; }
    }

    /// <summary>Resposta com jornadas e funis do produto.</summary>
    public sealed record Response(
        IReadOnlyList<JourneyDto> Journeys,
        decimal AverageCompletionRate,
        string MostCompletedJourney,
        string HighestDropOffJourney,
        string PeriodLabel);

    /// <summary>Jornada individual com steps e métricas.</summary>
    public sealed record JourneyDto(
        string JourneyId,
        string JourneyName,
        IReadOnlyList<JourneyStepDto> Steps,
        decimal CompletionRate,
        decimal AvgDurationMinutes,
        JourneyStatus Status,
        string BiggestDropOff);

    /// <summary>Step de uma jornada com taxa de conclusão.</summary>
    public sealed record JourneyStepDto(
        string StepId,
        string StepName,
        decimal CompletionPercent,
        int Order);
}
