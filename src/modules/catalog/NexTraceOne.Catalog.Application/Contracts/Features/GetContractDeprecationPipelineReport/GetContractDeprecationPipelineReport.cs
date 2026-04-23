using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractDeprecationPipelineReport;

/// <summary>
/// Feature: GetContractDeprecationPipelineReport — relatório de pipeline de deprecação de contratos.
///
/// Clasifica contratos em deprecação em <c>DeprecationPipelineTier</c>:
/// - OnTrack:  MigrationProgress ≥ deprecation_migration_on_track_pct E SunsetDeadline não expirado
/// - AtRisk:   MigrationProgress entre on_track e 0, ou SunsetDeadline dentro de sunset_warning_days
/// - Overdue:  SunsetDeadline expirado E ConsumerCount > 0
/// - Blocked:  DeprecationAge > deprecation_max_days sem progresso (MigrationProgress &lt; 5%)
///
/// <c>TenantDeprecationHealthScore</c> = % de contratos com tier OnTrack.
///
/// Wave AV.1 — Contract Lifecycle Automation &amp; Deprecation Intelligence (Catalog/ChangeGovernance).
/// </summary>
public static class GetContractDeprecationPipelineReport
{
    // ── Thresholds ─────────────────────────────────────────────────────────
    private const double OnTrackMigrationPct = 70.0;
    private const double BlockedMigrationPct = 5.0;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int DeprecationMaxDays = 180,
        int SunsetWarningDays = 30,
        double MinNotificationPct = 80.0) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DeprecationMaxDays).InclusiveBetween(1, 730);
            RuleFor(x => x.SunsetWarningDays).InclusiveBetween(1, 365);
            RuleFor(x => x.MinNotificationPct).InclusiveBetween(0.0, 100.0);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum DeprecationPipelineTier { OnTrack, AtRisk, Overdue, Blocked }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ContractDeprecationRow(
        Guid ContractId,
        string ContractName,
        string ContractVersion,
        string Protocol,
        string? OwnerTeamId,
        string ServiceId,
        double DeprecationAgeDays,
        DateTimeOffset? SunsetDeadline,
        double? DaysToSunset,
        int ConsumerCount,
        double NotifiedConsumersPct,
        double MigratedConsumersPct,
        double MigrationProgress,
        double? OwnerResponseTimeDays,
        IReadOnlyList<string> BlockingConsumerIds,
        DeprecationPipelineTier Tier);

    public sealed record NotificationGap(
        Guid ContractId,
        string ContractName,
        double NotifiedConsumersPct,
        int MissingNotifications);

    public sealed record MigrationBenchmark(
        Guid ContractId,
        string ContractName,
        double MigrationProgress);

    public sealed record TenantDeprecationPipelineSummary(
        int ActiveDeprecations,
        int ApproachingSunset,
        int OverdueSunsets,
        double TenantDeprecationHealthScore,
        int TotalBlockingConsumers);

    public sealed record Report(
        string TenantId,
        DateTimeOffset GeneratedAt,
        TenantDeprecationPipelineSummary Summary,
        IReadOnlyList<ContractDeprecationRow> Contracts,
        IReadOnlyList<NotificationGap> NotificationGaps,
        IReadOnlyList<MigrationBenchmark> FastestMigrations,
        IReadOnlyList<MigrationBenchmark> SlowestMigrations);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IContractDeprecationPipelineReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListDeprecatedContractsByTenantAsync(request.TenantId, cancellationToken);

            var rows = entries.Select(e => BuildRow(e, now, request)).ToList();

            var summary = BuildSummary(rows, request.SunsetWarningDays, now);

            var notifGaps = rows
                .Where(r => r.NotifiedConsumersPct < request.MinNotificationPct && r.ConsumerCount > 0)
                .Select(r => new NotificationGap(
                    r.ContractId,
                    r.ContractName,
                    r.NotifiedConsumersPct,
                    (int)Math.Ceiling(r.ConsumerCount * (request.MinNotificationPct - r.NotifiedConsumersPct) / 100.0)))
                .OrderBy(g => g.NotifiedConsumersPct)
                .ToList();

            var sorted = rows.OrderByDescending(r => r.MigrationProgress).ToList();
            var fastest = sorted.Take(3).Select(r => new MigrationBenchmark(r.ContractId, r.ContractName, r.MigrationProgress)).ToList();
            var slowest = sorted.TakeLast(3).Select(r => new MigrationBenchmark(r.ContractId, r.ContractName, r.MigrationProgress)).ToList();

            return Result<Report>.Success(new Report(
                request.TenantId,
                now,
                summary,
                rows,
                notifGaps,
                fastest,
                slowest));
        }

        private static ContractDeprecationRow BuildRow(
            IContractDeprecationPipelineReader.DeprecatedContractEntry e,
            DateTimeOffset now,
            Query q)
        {
            double ageDays = (now - e.DeprecatedAt).TotalDays;
            double? daysToSunset = e.SunsetDeadline.HasValue
                ? (e.SunsetDeadline.Value - now).TotalDays
                : null;

            double notifiedPct = e.TotalConsumers == 0 ? 100.0
                : Math.Round((double)e.NotifiedConsumers / e.TotalConsumers * 100.0, 2);
            double migratedPct = e.TotalConsumers == 0 ? 100.0
                : Math.Round((double)e.MigratedConsumers / e.TotalConsumers * 100.0, 2);
            double migrationProgress = migratedPct;

            double? ownerResponseDays = e.FirstNotificationSentAt.HasValue
                ? (e.FirstNotificationSentAt.Value - e.DeprecatedAt).TotalDays
                : null;

            bool sunsetExpired = daysToSunset.HasValue && daysToSunset.Value < 0;
            bool approachingSunset = daysToSunset.HasValue && daysToSunset.Value >= 0 && daysToSunset.Value <= q.SunsetWarningDays;

            DeprecationPipelineTier tier;
            if (sunsetExpired && e.TotalConsumers > 0)
                tier = DeprecationPipelineTier.Overdue;
            else if (ageDays > q.DeprecationMaxDays && migrationProgress < BlockedMigrationPct)
                tier = DeprecationPipelineTier.Blocked;
            else if (migrationProgress >= OnTrackMigrationPct && !sunsetExpired)
                tier = DeprecationPipelineTier.OnTrack;
            else
                tier = DeprecationPipelineTier.AtRisk;

            return new ContractDeprecationRow(
                e.ContractId,
                e.ContractName,
                e.ContractVersion,
                e.Protocol,
                e.OwnerTeamId,
                e.ServiceId,
                Math.Round(ageDays, 1),
                e.SunsetDeadline,
                daysToSunset.HasValue ? Math.Round(daysToSunset.Value, 1) : null,
                e.TotalConsumers,
                notifiedPct,
                migratedPct,
                migrationProgress,
                ownerResponseDays.HasValue ? Math.Round(ownerResponseDays.Value, 1) : null,
                e.BlockingConsumerIds,
                tier);
        }

        private static TenantDeprecationPipelineSummary BuildSummary(
            IReadOnlyList<ContractDeprecationRow> rows,
            int sunsetWarningDays,
            DateTimeOffset now)
        {
            int total = rows.Count;
            int onTrack = rows.Count(r => r.Tier == DeprecationPipelineTier.OnTrack);
            int approachingSunset = rows.Count(r => r.DaysToSunset.HasValue && r.DaysToSunset.Value >= 0 && r.DaysToSunset.Value <= sunsetWarningDays);
            int overdue = rows.Count(r => r.Tier == DeprecationPipelineTier.Overdue);
            int totalBlocking = rows.Sum(r => r.BlockingConsumerIds.Count);
            double healthScore = total == 0 ? 100.0 : Math.Round((double)onTrack / total * 100.0, 2);

            return new TenantDeprecationPipelineSummary(total, approachingSunset, overdue, healthScore, totalBlocking);
        }
    }
}
