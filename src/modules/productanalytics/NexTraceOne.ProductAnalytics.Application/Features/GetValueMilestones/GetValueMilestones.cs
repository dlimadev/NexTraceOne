using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetValueMilestones;

/// <summary>
/// Retorna marcos de valor atingidos pelos utilizadores.
/// Responde: quanto tempo até o primeiro valor? Quais milestones são mais atingidos?
/// Qual a progressão de valor por persona?
/// Consome dados reais do IAnalyticsEventRepository.
/// </summary>
public static class GetValueMilestones
{
    /// <summary>Query para marcos de valor.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna marcos de valor a partir de dados reais.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        private static readonly (ValueMilestoneType Type, string Name, AnalyticsEventType EventType)[] MilestoneDefs =
        [
            (ValueMilestoneType.FirstSearchSuccess, "First Search Success", AnalyticsEventType.SearchResultClicked),
            (ValueMilestoneType.FirstServiceLookup, "First Service Lookup", AnalyticsEventType.EntityViewed),
            (ValueMilestoneType.FirstContractView, "First Contract View", AnalyticsEventType.EntityViewed),
            (ValueMilestoneType.FirstContractDraftCreated, "First Contract Draft Created", AnalyticsEventType.ContractDraftCreated),
            (ValueMilestoneType.FirstContractPublished, "First Contract Published", AnalyticsEventType.ContractPublished),
            (ValueMilestoneType.FirstAiUsefulInteraction, "First AI Useful Interaction", AnalyticsEventType.AssistantResponseUsed),
            (ValueMilestoneType.FirstIncidentInvestigation, "First Incident Investigation", AnalyticsEventType.IncidentInvestigated),
            (ValueMilestoneType.FirstMitigationCompleted, "First Mitigation Completed", AnalyticsEventType.MitigationWorkflowCompleted),
            (ValueMilestoneType.FirstExecutiveOverviewConsumed, "First Executive Overview", AnalyticsEventType.ExecutiveOverviewViewed),
            (ValueMilestoneType.FirstRunbookConsulted, "First Runbook Consulted", AnalyticsEventType.RunbookViewed),
            (ValueMilestoneType.FirstSourceOfTruthUsed, "First Source of Truth Used", AnalyticsEventType.SourceOfTruthQueried),
            (ValueMilestoneType.FirstEvidenceExported, "First Evidence Exported", AnalyticsEventType.EvidencePackageExported),
            (ValueMilestoneType.FirstReportGenerated, "First Report Generated", AnalyticsEventType.ReportGenerated),
            (ValueMilestoneType.FirstReliabilityViewed, "First Reliability Viewed", AnalyticsEventType.ReliabilityDashboardViewed),
            (ValueMilestoneType.FirstAutomationCreated, "First Automation Created", AnalyticsEventType.OnboardingStepCompleted)
        ];

        private static readonly AnalyticsEventType[] FirstValueEventTypes =
        [
            AnalyticsEventType.SearchResultClicked,
            AnalyticsEventType.EntityViewed,
            AnalyticsEventType.AssistantResponseUsed,
            AnalyticsEventType.OnboardingStepCompleted
        ];

        private static readonly AnalyticsEventType[] CoreValueEventTypes =
        [
            AnalyticsEventType.ContractPublished,
            AnalyticsEventType.MitigationWorkflowCompleted
        ];

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range);

            var totalUsers = await repository.CountUniqueUsersAsync(
                persona: request.Persona, module: null, teamId: request.TeamId, domainId: null,
                from, to, cancellationToken);

            if (totalUsers == 0)
            {
                var emptyMilestones = MilestoneDefs
                    .Select(d => new MilestoneDto(d.Type, d.Name, 0m, 0m, 0, TrendDirection.Stable))
                    .ToArray();

                return Result<Response>.Success(new Response(
                    Milestones: emptyMilestones,
                    AvgTimeToFirstValueMinutes: 0m,
                    AvgTimeToCoreValueMinutes: 0m,
                    OverallCompletionRate: 0m,
                    FastestMilestone: ValueMilestoneType.FirstSearchSuccess,
                    SlowestMilestone: ValueMilestoneType.FirstSearchSuccess,
                    PeriodLabel: periodLabel));
            }

            var allEventTypes = MilestoneDefs.Select(d => d.EventType).Distinct().ToArray();

            var userCounts = await repository.CountUsersByEventTypeAsync(
                allEventTypes, request.Persona, request.TeamId, from, to, cancellationToken);

            var userCountDict = userCounts.ToDictionary(u => u.EventType, u => u.UniqueUsers);

            var userFirstEvents = await repository.GetUserFirstEventTimesAsync(
                allEventTypes, request.Persona, request.TeamId, from, to, cancellationToken);

            var (previousFrom, previousTo, _) = ResolveRange(from, request.Range);
            var previousUserCounts = await repository.CountUsersByEventTypeAsync(
                allEventTypes, request.Persona, request.TeamId, previousFrom, previousTo, cancellationToken);

            var previousDict = previousUserCounts.ToDictionary(u => u.EventType, u => u.UniqueUsers);

            var avgTimeByEventType = userFirstEvents
                .GroupBy(e => e.EventType)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var userIds = g.Select(x => x.UserId).Distinct().ToList();
                        var allUserFirstTimes = userFirstEvents
                            .Where(x => userIds.Contains(x.UserId))
                            .GroupBy(x => x.UserId)
                            .Select(ug => ug.Min(x => x.FirstOccurrence))
                            .ToList();

                        var deltas = new List<decimal>();
                        foreach (var userId in userIds)
                        {
                            var userGlobalFirst = allUserFirstTimes
                                .Where(t => userFirstEvents.Any(x => x.UserId == userId))
                                .DefaultIfEmpty(DateTimeOffset.MinValue)
                                .Min();

                            if (userGlobalFirst == DateTimeOffset.MinValue) continue;

                            var milestoneFirst = g.Where(x => x.UserId == userId).Min(x => x.FirstOccurrence);
                            var delta = (decimal)(milestoneFirst - userGlobalFirst).TotalMinutes;
                            if (delta >= 0) deltas.Add(delta);
                        }

                        return deltas.Count > 0 ? Math.Round(deltas.Average(), 1) : 0m;
                    });

            var milestones = MilestoneDefs.Select(def =>
            {
                var usersReached = userCountDict.GetValueOrDefault(def.EventType, 0);
                var completionRate = totalUsers > 0
                    ? Math.Round((usersReached / (decimal)totalUsers) * 100m, 1)
                    : 0m;

                var avgTime = avgTimeByEventType.GetValueOrDefault(def.EventType, 0m);

                var previousReached = previousDict.GetValueOrDefault(def.EventType, 0);
                var trend = ClassifyTrend(usersReached, previousReached);

                return new MilestoneDto(def.Type, def.Name, completionRate, avgTime, usersReached, trend);
            }).ToArray();

            var firstValueTimes = userFirstEvents
                .Where(e => FirstValueEventTypes.Contains(e.EventType))
                .GroupBy(e => e.UserId)
                .Select(g => (decimal)(g.Min(x => x.FirstOccurrence) - from).TotalMinutes)
                .Where(t => t >= 0)
                .ToList();

            var coreValueTimes = userFirstEvents
                .Where(e => CoreValueEventTypes.Contains(e.EventType))
                .GroupBy(e => e.UserId)
                .Select(g => (decimal)(g.Min(x => x.FirstOccurrence) - from).TotalMinutes)
                .Where(t => t >= 0)
                .ToList();

            var avgTtfv = firstValueTimes.Count > 0 ? Math.Round(firstValueTimes.Average(), 1) : 0m;
            var avgTtcore = coreValueTimes.Count > 0 ? Math.Round(coreValueTimes.Average(), 1) : 0m;

            var overallCompletion = milestones.Length > 0
                ? Math.Round(milestones.Average(m => m.CompletionRate), 1)
                : 0m;

            var fastest = milestones
                .Where(m => m.UsersReached > 0)
                .OrderBy(m => m.AvgTimeToReachMinutes)
                .FirstOrDefault()?.MilestoneType ?? ValueMilestoneType.FirstSearchSuccess;

            var slowest = milestones
                .Where(m => m.UsersReached > 0)
                .OrderByDescending(m => m.AvgTimeToReachMinutes)
                .FirstOrDefault()?.MilestoneType ?? ValueMilestoneType.FirstSearchSuccess;

            return Result<Response>.Success(new Response(
                Milestones: milestones,
                AvgTimeToFirstValueMinutes: avgTtfv,
                AvgTimeToCoreValueMinutes: avgTtcore,
                OverallCompletionRate: overallCompletion,
                FastestMilestone: fastest,
                SlowestMilestone: slowest,
                PeriodLabel: periodLabel));
        }

        private static TrendDirection ClassifyTrend(int current, int previous)
        {
            if (previous == 0) return current == 0 ? TrendDirection.Stable : TrendDirection.Improving;
            var delta = (current - previous) / (decimal)previous;
            if (delta >= 0.05m) return TrendDirection.Improving;
            if (delta <= -0.05m) return TrendDirection.Declining;
            return TrendDirection.Stable;
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
    }

    /// <summary>Resposta com marcos de valor.</summary>
    public sealed record Response(
        IReadOnlyList<MilestoneDto> Milestones,
        decimal AvgTimeToFirstValueMinutes,
        decimal AvgTimeToCoreValueMinutes,
        decimal OverallCompletionRate,
        ValueMilestoneType FastestMilestone,
        ValueMilestoneType SlowestMilestone,
        string PeriodLabel);

    /// <summary>Marco de valor individual com métricas.</summary>
    public sealed record MilestoneDto(
        ValueMilestoneType MilestoneType,
        string MilestoneName,
        decimal CompletionRate,
        decimal AvgTimeToReachMinutes,
        int UsersReached,
        TrendDirection Trend);
}
