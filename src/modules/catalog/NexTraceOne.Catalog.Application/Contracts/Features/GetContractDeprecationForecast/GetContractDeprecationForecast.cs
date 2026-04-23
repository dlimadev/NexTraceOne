using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractDeprecationForecast;

/// <summary>
/// Feature: GetContractDeprecationForecast — previsão de contratos candidatos a deprecação.
///
/// Calcula <c>DeprecationProbabilityScore</c> (0–100) por contrato activo:
/// - AgeRisk            (35%) — idade superior a contract_max_age_days
/// - SuccessorAvailable (30%) — existe versão mais recente já activa
/// - ConsumerDecline    (25%) — declínio mensal ≥ consumer_decline_pct_threshold%
/// - OwnerSignal        (10%) — owner já agendou via ScheduleContractDeprecation
///
/// Produz <c>ForecastedDeprecationCandidates</c> ordenados por score DESC.
/// Produz <c>TenantDeprecationOutlook</c> com contagem prevista em 30/60/90 dias.
///
/// Wave AV.3 — Contract Lifecycle Automation &amp; Deprecation Intelligence (Catalog/Foundation).
/// </summary>
public static class GetContractDeprecationForecast
{
    // ── Score weights ──────────────────────────────────────────────────────
    private const double AgeRiskWeight = 0.35;
    private const double SuccessorWeight = 0.30;
    private const double ConsumerDeclineWeight = 0.25;
    private const double OwnerSignalWeight = 0.10;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int ContractMaxAgeDays = 365,
        double ConsumerDeclinePctThreshold = 20.0) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractMaxAgeDays).InclusiveBetween(1, 1825);
            RuleFor(x => x.ConsumerDeclinePctThreshold).InclusiveBetween(0.0, 100.0);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ForecastedCandidate(
        Guid ContractId,
        string ContractName,
        string ContractVersion,
        string Protocol,
        string? OwnerTeamId,
        double ContractAgeDays,
        bool HasSuccessorVersion,
        double ConsumerDeclinePct,
        bool OwnerSignalledDeprecation,
        double AgeRiskScore,
        double SuccessorScore,
        double ConsumerDeclineScore,
        double OwnerSignalScore,
        double DeprecationProbabilityScore);

    public sealed record TenantDeprecationOutlook(
        int EstimatedIn30Days,
        int EstimatedIn60Days,
        int EstimatedIn90Days,
        int EstimatedConsumersImpacted30Days,
        int EstimatedConsumersImpacted60Days,
        int EstimatedConsumersImpacted90Days);

    public sealed record PlannedDeprecationEntry(
        Guid ContractId,
        string ContractName,
        DateTimeOffset PlannedDeprecationDate,
        DateTimeOffset? PlannedSunsetDate,
        int ActiveConsumerCount);

    public sealed record Report(
        string TenantId,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<ForecastedCandidate> ForecastedDeprecationCandidates,
        TenantDeprecationOutlook DeprecationOutlook,
        IReadOnlyList<PlannedDeprecationEntry> PlannedDeprecationCalendar);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IContractDeprecationForecastReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListActiveContractsByTenantAsync(request.TenantId, cancellationToken);

            var candidates = entries
                .Select(e => BuildCandidate(e, now, request))
                .Where(c => c.DeprecationProbabilityScore > 0)
                .OrderByDescending(c => c.DeprecationProbabilityScore)
                .ToList();

            var outlook = BuildOutlook(candidates, now, entries);

            // PlannedDeprecationCalendar comes from entries that have planned deprecations
            var planned = entries
                .SelectMany(e => e.PlannedDeprecations)
                .Select(p => new PlannedDeprecationEntry(
                    p.ContractId,
                    p.ContractName,
                    p.PlannedDeprecationDate,
                    p.PlannedSunsetDate,
                    p.ActiveConsumerCount))
                .OrderBy(p => p.PlannedDeprecationDate)
                .ToList();

            return Result<Report>.Success(new Report(request.TenantId, now, candidates, outlook, planned));
        }

        private static ForecastedCandidate BuildCandidate(
            IContractDeprecationForecastReader.ActiveContractForecastEntry e,
            DateTimeOffset now,
            Query q)
        {
            double ageDays = (now - e.CreatedAt).TotalDays;

            // AgeRisk score (0–100): max at 2x max_age_days
            double ageScore = ageDays >= q.ContractMaxAgeDays
                ? Math.Min(100.0, Math.Round(ageDays / q.ContractMaxAgeDays * 50.0, 2))
                : 0.0;

            double successorScore = e.HasSuccessorVersion ? 100.0 : 0.0;

            // ConsumerDecline: % decline month-over-month
            double declinePct = 0.0;
            if (e.ConsumerCountTwoMonthsAgo > 0)
            {
                declinePct = Math.Max(0.0,
                    (e.ConsumerCountTwoMonthsAgo - e.ConsumerCountPrevMonth) / (double)e.ConsumerCountTwoMonthsAgo * 100.0);
            }
            double consumerDeclineScore = declinePct >= q.ConsumerDeclinePctThreshold ? 100.0
                : Math.Round(declinePct / Math.Max(1.0, q.ConsumerDeclinePctThreshold) * 100.0, 2);

            double ownerScore = e.OwnerSignalledDeprecation ? 100.0 : 0.0;

            double total = Math.Round(
                ageScore * AgeRiskWeight +
                successorScore * SuccessorWeight +
                consumerDeclineScore * ConsumerDeclineWeight +
                ownerScore * OwnerSignalWeight, 2);

            return new ForecastedCandidate(
                e.ContractId,
                e.ContractName,
                e.ContractVersion,
                e.Protocol,
                e.OwnerTeamId,
                Math.Round(ageDays, 1),
                e.HasSuccessorVersion,
                Math.Round(declinePct, 2),
                e.OwnerSignalledDeprecation,
                ageScore,
                successorScore,
                consumerDeclineScore,
                ownerScore,
                total);
        }

        private static TenantDeprecationOutlook BuildOutlook(
            IReadOnlyList<ForecastedCandidate> candidates,
            DateTimeOffset now,
            IReadOnlyList<IContractDeprecationForecastReader.ActiveContractForecastEntry> entries)
        {
            // High probability (≥ 70) → likely in 30d; medium (≥ 45) → 60d; low (≥ 25) → 90d
            const double high = 70.0;
            const double medium = 45.0;
            const double low = 25.0;

            var high30 = candidates.Where(c => c.DeprecationProbabilityScore >= high).ToList();
            var medium60 = candidates.Where(c => c.DeprecationProbabilityScore >= medium).ToList();
            var low90 = candidates.Where(c => c.DeprecationProbabilityScore >= low).ToList();

            // Approximate consumers impacted
            var entryMap = entries.ToDictionary(e => e.ContractId);
            int consumers30 = high30.Sum(c => entryMap.TryGetValue(c.ContractId, out var e) ? e.CurrentConsumerCount : 0);
            int consumers60 = medium60.Sum(c => entryMap.TryGetValue(c.ContractId, out var e) ? e.CurrentConsumerCount : 0);
            int consumers90 = low90.Sum(c => entryMap.TryGetValue(c.ContractId, out var e) ? e.CurrentConsumerCount : 0);

            return new TenantDeprecationOutlook(
                high30.Count, medium60.Count, low90.Count,
                consumers30, consumers60, consumers90);
        }
    }
}
