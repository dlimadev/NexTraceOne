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

namespace NexTraceOne.ProductAnalytics.Application.Features.GetCohortAnalysis;

/// <summary>
/// Analisa cohorts de utilizadores agrupados por data do primeiro evento.
/// Responde: qual a taxa de retenção por semana/mês após a primeira acção?
/// Quais cohorts convertem mais rapidamente para o primeiro valor real?
/// </summary>
public static class GetCohortAnalysis
{
    /// <summary>Query para análise de cohorts.</summary>
    public sealed record Query(
        string? Granularity,    // "week" | "month" — default: "week"
        int? Periods,           // número de períodos a mostrar — default: 8
        string? Metric,         // "retention" | "activation" — default: "retention"
        string? Persona,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que computa a análise de cohorts a partir de dados de eventos.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxRangeCfg = await configService.ResolveEffectiveValueAsync(
                AnalyticsConfigKeys.MaxRangeDays, ConfigurationScope.System, null, cancellationToken);
            var maxRangeDays = int.TryParse(maxRangeCfg?.EffectiveValue, out var mrd) ? mrd : AnalyticsConstants.MaxRangeDays;

            var (from, to, periodLabel) = AnalyticsQueryHelper.ResolveRange(clock.UtcNow, request.Range, maxRangeDays, defaultRange: "last_90d");

            var granularity = NormalizeGranularity(request.Granularity);
            var periods = Math.Clamp(request.Periods ?? AnalyticsConstants.DefaultCohortPeriods, 1, AnalyticsConstants.MaxCohortPeriods);
            var metric = NormalizeMetric(request.Metric);

            // Fetch first event per user in range
            var activationEventTypes = new[] { AnalyticsEventType.ModuleViewed };
            var retentionEventTypes = new[]
            {
                AnalyticsEventType.SearchExecuted,
                AnalyticsEventType.EntityViewed,
                AnalyticsEventType.AssistantPromptSubmitted,
                AnalyticsEventType.ContractDraftCreated,
                AnalyticsEventType.IncidentInvestigated,
                AnalyticsEventType.OnboardingStepCompleted
            };

            var targetEventTypes = metric == "activation" ? activationEventTypes : retentionEventTypes;

            var userFirstEvents = await repository.GetUserFirstEventTimesAsync(
                targetEventTypes, request.Persona, null, from, to, cancellationToken);

            if (userFirstEvents.Count == 0)
            {
                return new Response(
                    Granularity: granularity,
                    Metric: metric,
                    Periods: periods,
                    Cohorts: [],
                    PeriodLabel: periodLabel);
            }

            // Group users by cohort period (week/month of first event)
            var cohortGroups = userFirstEvents
                .GroupBy(u => GetCohortBucket(u.FirstOccurrence, granularity))
                .OrderBy(g => g.Key)
                .Take(periods)
                .ToList();

            var cohorts = new List<CohortDto>();

            foreach (var cohortGroup in cohortGroups)
            {
                var cohortStart = cohortGroup.Key;
                var cohortEnd = GetCohortEnd(cohortStart, granularity);
                var cohortSize = cohortGroup.Select(u => u.UserId).Distinct().Count();

                // For each subsequent period, count how many users from this cohort returned
                var retentionByPeriod = new List<CohortPeriodDto>();

                for (var p = 0; p < periods; p++)
                {
                    var periodStart = cohortStart.AddDays(p * GetPeriodDays(granularity));
                    var periodEnd = periodStart.AddDays(GetPeriodDays(granularity));

                    if (periodStart > to) break;

                    // Users from this cohort who had events in this period
                    var cohortUserIds = cohortGroup.Select(u => u.UserId).ToHashSet();

                    // For period 0 it's 100% by definition
                    if (p == 0)
                    {
                        retentionByPeriod.Add(new CohortPeriodDto(p, 100m, cohortSize));
                        continue;
                    }

                    // Count distinct users from this cohort who appear with events in this period window
                    var returnedUsers = userFirstEvents
                        .Where(u => cohortUserIds.Contains(u.UserId)
                            && u.FirstOccurrence >= periodStart
                            && u.FirstOccurrence < periodEnd)
                        .Select(u => u.UserId)
                        .Distinct()
                        .Count();

                    var rate = cohortSize > 0
                        ? Math.Round((returnedUsers / (decimal)cohortSize) * 100m, 1)
                        : 0m;

                    retentionByPeriod.Add(new CohortPeriodDto(p, rate, returnedUsers));
                }

                cohorts.Add(new CohortDto(
                    CohortLabel: FormatCohortLabel(cohortStart, granularity),
                    CohortStart: cohortStart,
                    CohortEnd: cohortEnd,
                    CohortSize: cohortSize,
                    Periods: retentionByPeriod));
            }

            var avgRetentionFirstPeriod = cohorts.Count > 0 && cohorts.All(c => c.Periods.Count > 1)
                ? Math.Round(cohorts.Average(c => c.Periods.Count > 1 ? c.Periods[1].RetentionRate : 0m), 1)
                : 0m;

            return new Response(
                Granularity: granularity,
                Metric: metric,
                Periods: periods,
                Cohorts: cohorts,
                PeriodLabel: periodLabel,
                AvgRetentionFirstPeriod: avgRetentionFirstPeriod);
        }

        private static string NormalizeGranularity(string? granularity)
            => granularity?.ToLowerInvariant() switch
            {
                "month" => "month",
                _ => "week"
            };

        private static string NormalizeMetric(string? metric)
            => metric?.ToLowerInvariant() switch
            {
                "activation" => "activation",
                _ => "retention"
            };

        private static int GetPeriodDays(string granularity)
            => granularity == "month" ? 30 : 7;

        private static DateTimeOffset GetCohortBucket(DateTimeOffset date, string granularity)
        {
            if (granularity == "month")
                return new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, date.Offset);

            // Start of week (Monday)
            var dayOfWeek = (int)date.DayOfWeek;
            var daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            var weekStart = date.AddDays(-daysToMonday);
            return new DateTimeOffset(weekStart.Year, weekStart.Month, weekStart.Day, 0, 0, 0, date.Offset);
        }

        private static DateTimeOffset GetCohortEnd(DateTimeOffset cohortStart, string granularity)
            => granularity == "month"
                ? cohortStart.AddMonths(1).AddSeconds(-1)
                : cohortStart.AddDays(7).AddSeconds(-1);

        private static string FormatCohortLabel(DateTimeOffset cohortStart, string granularity)
            => granularity == "month"
                ? cohortStart.ToString("MMM yyyy")
                : $"W{GetIsoWeek(cohortStart)} {cohortStart:yyyy}";

        private static int GetIsoWeek(DateTimeOffset date)
        {
            var day = date.DayOfWeek;
            var thursday = date.AddDays(day == DayOfWeek.Sunday ? -3 : DayOfWeek.Thursday - day);
            return (thursday.DayOfYear - 1) / 7 + 1;
        }

    }

    /// <summary>Resposta com análise de cohorts.</summary>
    public sealed record Response(
        string Granularity,
        string Metric,
        int Periods,
        IReadOnlyList<CohortDto> Cohorts,
        string PeriodLabel,
        decimal AvgRetentionFirstPeriod = 0m);

    /// <summary>Cohort individual com retenção por período.</summary>
    public sealed record CohortDto(
        string CohortLabel,
        DateTimeOffset CohortStart,
        DateTimeOffset CohortEnd,
        int CohortSize,
        IReadOnlyList<CohortPeriodDto> Periods);

    /// <summary>Taxa de retenção para um período específico de um cohort.</summary>
    public sealed record CohortPeriodDto(
        int PeriodIndex,
        decimal RetentionRate,
        int ActiveUsers);
}
