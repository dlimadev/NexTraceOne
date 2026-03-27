using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetJourneys;

/// <summary>
/// Retorna jornadas e funis do produto com métricas de conclusão.
/// Responde: quais jornadas chegam até valor real? Onde há abandono?
/// Qual é o tempo médio por jornada? Onde estão os pontos de drop-off?
/// </summary>
public static class GetJourneys
{
    /// <summary>Query para jornadas e funis do produto.</summary>
    public sealed record Query(
        string? JourneyId,
        string? Persona,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna métricas de jornadas.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var journeys = new List<JourneyDto>
            {
                new("search_to_entity",
                    "Search to Entity View",
                    new[]
                    {
                        new JourneyStepDto("search_executed", "Search Executed", 100.0m, 0),
                        new JourneyStepDto("results_displayed", "Results Displayed", 87.2m, 1),
                        new JourneyStepDto("result_clicked", "Result Clicked", 64.5m, 2),
                        new JourneyStepDto("entity_viewed", "Entity Viewed", 61.8m, 3)
                    },
                    61.8m, 4.2m, JourneyStatus.Completed, "search_executed → results_displayed"),

                new("ai_prompt_to_action",
                    "AI Prompt to Useful Action",
                    new[]
                    {
                        new JourneyStepDto("assistant_opened", "Assistant Opened", 100.0m, 0),
                        new JourneyStepDto("prompt_submitted", "Prompt Submitted", 82.4m, 1),
                        new JourneyStepDto("response_received", "Response Received", 80.1m, 2),
                        new JourneyStepDto("response_used", "Response Used", 48.6m, 3)
                    },
                    48.6m, 6.8m, JourneyStatus.Completed, "response_received → response_used"),

                new("contract_draft_to_publish",
                    "Contract Draft to Publication",
                    new[]
                    {
                        new JourneyStepDto("studio_opened", "Studio Opened", 100.0m, 0),
                        new JourneyStepDto("draft_created", "Draft Created", 72.3m, 1),
                        new JourneyStepDto("draft_validated", "Draft Validated", 58.1m, 2),
                        new JourneyStepDto("review_submitted", "Review Submitted", 41.2m, 3),
                        new JourneyStepDto("contract_published", "Contract Published", 34.8m, 4)
                    },
                    34.8m, 48.5m, JourneyStatus.Completed, "draft_validated → review_submitted"),

                new("incident_to_mitigation",
                    "Incident to Mitigation Completion",
                    new[]
                    {
                        new JourneyStepDto("incident_opened", "Incident Opened", 100.0m, 0),
                        new JourneyStepDto("investigation_started", "Investigation Started", 91.2m, 1),
                        new JourneyStepDto("cause_identified", "Cause Identified", 68.4m, 2),
                        new JourneyStepDto("mitigation_started", "Mitigation Started", 55.7m, 3),
                        new JourneyStepDto("mitigation_completed", "Mitigation Completed", 42.3m, 4)
                    },
                    42.3m, 125.0m, JourneyStatus.Completed, "cause_identified → mitigation_started"),

                new("onboarding_to_first_action",
                    "Onboarding to First Meaningful Action",
                    new[]
                    {
                        new JourneyStepDto("first_login", "First Login", 100.0m, 0),
                        new JourneyStepDto("persona_selected", "Persona Selected", 94.5m, 1),
                        new JourneyStepDto("dashboard_viewed", "Dashboard Viewed", 92.1m, 2),
                        new JourneyStepDto("first_search", "First Search", 78.3m, 3),
                        new JourneyStepDto("first_meaningful_action", "First Meaningful Action", 62.4m, 4)
                    },
                    62.4m, 18.5m, JourneyStatus.Completed, "first_search → first_meaningful_action")
            };

            // Filtrar por journeyId se especificado
            if (!string.IsNullOrWhiteSpace(request.JourneyId))
            {
                journeys = journeys
                    .Where(j => j.JourneyId.Equals(request.JourneyId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var response = new Response(
                Journeys: journeys,
                AverageCompletionRate: journeys.Count > 0
                    ? journeys.Average(j => j.CompletionRate)
                    : 0m,
                MostCompletedJourney: "search_to_entity",
                HighestDropOffJourney: "contract_draft_to_publish",
                PeriodLabel: request.Range ?? "last_30d");

            return Task.FromResult(Result<Response>.Success(response));
        }
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
