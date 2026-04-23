using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application.Features.GetKnowledgeBaseUtilizationReport;

/// <summary>
/// Feature: GetKnowledgeBaseUtilizationReport — análise de utilização do knowledge hub.
///
/// Classifica o knowledge hub por <c>KnowledgeHubHealthTier</c>:
/// - Thriving: KnowledgeResolutionRate ≥ thriving_rate AND KnowledgeGapCount ≤ gap_count_thriving
/// - Active: KnowledgeResolutionRate ≥ 50% AND GapCount razoável
/// - Underused: DailyActiveUsers muito baixo ou SearchVolume quase nulo
/// - Gap-Heavy: muitos termos sem resultado
///
/// Wave AY.2 — Organizational Knowledge &amp; Documentation Intelligence.
/// </summary>
public static class GetKnowledgeBaseUtilizationReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int TopSearchTermsCount = 20,
        int TopKnowledgeGapsCount = 10,
        int TopDocumentsCount = 10,
        decimal ThriveResolutionRatePct = 70m,
        int ThriveGapCountMax = 10) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 365);
            RuleFor(x => x.TopSearchTermsCount).InclusiveBetween(1, 50);
            RuleFor(x => x.TopKnowledgeGapsCount).InclusiveBetween(1, 50);
            RuleFor(x => x.TopDocumentsCount).InclusiveBetween(1, 50);
            RuleFor(x => x.ThriveResolutionRatePct).InclusiveBetween(0m, 100m);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum KnowledgeHubHealthTier { Thriving, Active, Underused, GapHeavy }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record SearchTermSummary(
        string Term,
        int SearchCount,
        bool HasResults,
        decimal ClickThroughRate);

    public sealed record WeeklySearchVolume(
        int WeekOffset,
        int SearchCount);

    public sealed record DocumentAccessSummary(
        string DocumentId,
        string Title,
        string Category,
        int AccessCount);

    public sealed record RunbookAccessSummary(
        string ServiceId,
        string ServiceName,
        int AccessCount);

    public sealed record KnowledgeHubUtilizationSummary(
        int DailyActiveKnowledgeUsers,
        decimal SearchPerUserPerWeek,
        decimal KnowledgeResolutionRate,
        int KnowledgeGapCount,
        KnowledgeHubHealthTier KnowledgeHubHealthTier);

    public sealed record Report(
        string TenantId,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        IReadOnlyList<SearchTermSummary> TopSearchTerms,
        IReadOnlyList<SearchTermSummary> SearchTermsWithNoResults,
        IReadOnlyList<SearchTermSummary> SearchTermsWithLowRelevance,
        IReadOnlyList<WeeklySearchVolume> SearchVolumeTrend,
        IReadOnlyList<string> TopKnowledgeGaps,
        IReadOnlyList<DocumentAccessSummary> MostAccessedDocuments,
        IReadOnlyList<RunbookAccessSummary> MostAccessedRunbooks,
        KnowledgeHubUtilizationSummary Summary);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IKnowledgeBaseUtilizationReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var from = now.AddDays(-request.LookbackDays);

            var data = await reader.ReadByTenantAsync(request.TenantId, from, now, cancellationToken);

            var topSearchTerms = data.SearchTerms
                .OrderByDescending(t => t.SearchCount)
                .Take(request.TopSearchTermsCount)
                .Select(t => new SearchTermSummary(
                    t.Term, t.SearchCount, t.ResultCount > 0,
                    t.SearchCount > 0 ? Math.Round((decimal)t.ClickCount / t.SearchCount * 100, 1) : 0m))
                .ToList();

            var noResults = data.SearchTerms
                .Where(t => t.ResultCount == 0)
                .OrderByDescending(t => t.SearchCount)
                .Select(t => new SearchTermSummary(t.Term, t.SearchCount, false, 0m))
                .ToList();

            var lowRelevance = data.SearchTerms
                .Where(t => t.ResultCount > 0 && t.SearchCount > 0 &&
                            (decimal)t.ClickCount / t.SearchCount < 0.20m)
                .OrderByDescending(t => t.SearchCount)
                .Select(t => new SearchTermSummary(
                    t.Term, t.SearchCount, true,
                    Math.Round((decimal)t.ClickCount / t.SearchCount * 100, 1)))
                .ToList();

            var topGaps = noResults
                .Take(request.TopKnowledgeGapsCount)
                .Select(t => t.Term)
                .ToList();

            var mostAccessedDocs = data.AccessedDocuments
                .OrderByDescending(d => d.AccessCount)
                .Take(request.TopDocumentsCount)
                .Select(d => new DocumentAccessSummary(d.DocumentId, d.Title, d.Category, d.AccessCount))
                .ToList();

            var mostAccessedRunbooks = data.AccessedRunbooks
                .OrderByDescending(r => r.AccessCount)
                .Take(request.TopDocumentsCount)
                .Select(r => new RunbookAccessSummary(r.ServiceId, r.ServiceName, r.AccessCount))
                .ToList();

            // Weekly trend: group searches per week (approximate via even distribution)
            var weekCount = Math.Max(1, request.LookbackDays / 7);
            var totalSearches = data.SearchTerms.Sum(t => t.SearchCount);
            var searchVolumeTrend = Enumerable.Range(0, Math.Min(weekCount, 4))
                .Select(w => new WeeklySearchVolume(w, totalSearches / weekCount))
                .ToList();

            var summary = BuildSummary(data, noResults.Count, request);

            return Result<Report>.Success(new Report(
                request.TenantId, from, now, now, request.LookbackDays,
                topSearchTerms, noResults, lowRelevance,
                searchVolumeTrend,
                topGaps,
                mostAccessedDocs, mostAccessedRunbooks,
                summary));
        }

        internal static KnowledgeHubUtilizationSummary BuildSummary(
            IKnowledgeBaseUtilizationReader.KnowledgeBaseUtilizationData data,
            int gapCount,
            Query request)
        {
            var resolutionRate = data.TotalSearchSessions > 0
                ? Math.Round((decimal)data.SessionsWithResultClick / data.TotalSearchSessions * 100, 1)
                : 0m;

            var searchPerUserPerWeek = data.DailyActiveKnowledgeUsers > 0
                ? Math.Round(
                    (decimal)data.SearchTerms.Sum(t => t.SearchCount) /
                    data.DailyActiveKnowledgeUsers / Math.Max(1, request.LookbackDays / 7m), 1)
                : 0m;

            var tier = ComputeTier(resolutionRate, gapCount, data.DailyActiveKnowledgeUsers, request);

            return new KnowledgeHubUtilizationSummary(
                data.DailyActiveKnowledgeUsers,
                searchPerUserPerWeek,
                resolutionRate,
                gapCount,
                tier);
        }

        internal static KnowledgeHubHealthTier ComputeTier(
            decimal resolutionRate,
            int gapCount,
            int dailyActiveUsers,
            Query request)
        {
            if (gapCount > request.ThriveGapCountMax * 2)
                return KnowledgeHubHealthTier.GapHeavy;
            if (dailyActiveUsers == 0 || resolutionRate < 20m)
                return KnowledgeHubHealthTier.Underused;
            if (resolutionRate >= request.ThriveResolutionRatePct && gapCount <= request.ThriveGapCountMax)
                return KnowledgeHubHealthTier.Thriving;
            if (resolutionRate >= 50m)
                return KnowledgeHubHealthTier.Active;
            return KnowledgeHubHealthTier.Underused;
        }
    }
}
