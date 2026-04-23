using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Application.Features.GetNotificationEffectivenessReport;

/// <summary>
/// Feature: GetNotificationEffectivenessReport — análise de eficácia das notificações.
///
/// Cruza NotificationOutbox (notificações enviadas) com AuditEvent (ações do utilizador)
/// para calcular ActionRate por tipo de evento e canal.
///
/// Classifica cada EventType por <c>EffectivenessTier</c>:
/// - HighImpact  — ActionRatePct ≥ 60%
/// - Moderate    — ActionRatePct ≥ 30%
/// - LowImpact   — ActionRatePct ≥ 10%
/// - Noise       — ActionRatePct &lt; 10%
///
/// Detecta <c>AlertFatigueCandidates</c>: EventTypes Noise com volume &gt; <c>noise_volume_threshold</c>.
///
/// Wave AK.3 — Developer Experience &amp; Notification Management (Notifications / OI).
/// </summary>
public static class GetNotificationEffectivenessReport
{
    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal HighImpactThreshold = 60m;
    private const decimal ModerateThreshold = 30m;
    private const decimal LowImpactThreshold = 10m;

    internal const int DefaultLookbackDays = 30;
    internal const int DefaultActionWindowHours = 4;
    internal const int DefaultNoiseVolumeThreshold = 20;

    // ── Query ──────────────────────────────────────────────────────────────

    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        int ActionWindowHours = DefaultActionWindowHours,
        int NoiseVolumeThreshold = DefaultNoiseVolumeThreshold) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(x => x.ActionWindowHours).InclusiveBetween(1, 72);
            RuleFor(x => x.NoiseVolumeThreshold).GreaterThanOrEqualTo(1);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    public enum EffectivenessTier { HighImpact, Moderate, LowImpact, Noise }

    // ── Response records ───────────────────────────────────────────────────

    public sealed record EventTypeEffectivenessRow(
        string EventType,
        string ChannelType,
        int NotificationCount,
        decimal ActionRatePct,
        decimal SilenceRatePct,
        decimal MedianTimeToActionMinutes,
        EffectivenessTier Tier);

    public sealed record AlertFatigueCandidate(
        string EventType,
        int NotificationCount,
        decimal ActionRatePct,
        string RecommendedAction);

    public sealed record Report(
        string TenantId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        IReadOnlyList<EventTypeEffectivenessRow> ByEventType,
        IReadOnlyList<AlertFatigueCandidate> AlertFatigueCandidates,
        IReadOnlyList<string> TopEffectiveChannels,
        decimal TenantNotificationHealthScore,
        IReadOnlyList<string> RecommendedAdjustments,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        INotificationEffectivenessReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var periodStart = now.AddDays(-request.LookbackDays);

            var rawData = await reader.GetEffectivenessDataAsync(
                request.TenantId,
                periodStart,
                now,
                request.ActionWindowHours,
                cancellationToken);

            var rows = rawData
                .Select(d =>
                {
                    var total = d.NotificationCount;
                    var actionRate = total == 0 ? 0m : Math.Round((decimal)d.ActionCount / total * 100m, 2);
                    var silenceRate = total == 0 ? 0m : Math.Round((decimal)d.SilenceCount / total * 100m, 2);
                    var tier = actionRate >= HighImpactThreshold ? EffectivenessTier.HighImpact
                        : actionRate >= ModerateThreshold ? EffectivenessTier.Moderate
                        : actionRate >= LowImpactThreshold ? EffectivenessTier.LowImpact
                        : EffectivenessTier.Noise;
                    return new EventTypeEffectivenessRow(
                        d.EventType, d.ChannelType, d.NotificationCount,
                        actionRate, silenceRate, d.MedianTimeToActionMinutes, tier);
                })
                .OrderByDescending(r => r.ActionRatePct)
                .ToList();

            // Alert fatigue: Noise tier AND volume > threshold
            var fatigueCandidates = rows
                .Where(r => r.Tier == EffectivenessTier.Noise && r.NotificationCount > request.NoiseVolumeThreshold)
                .GroupBy(r => r.EventType)
                .Select(g =>
                {
                    var totalCount = g.Sum(r => r.NotificationCount);
                    var avgAction = g.Average(r => r.ActionRatePct);
                    return new AlertFatigueCandidate(
                        g.Key,
                        totalCount,
                        Math.Round(avgAction, 2),
                        "Consider increasing threshold or adding silence period.");
                })
                .OrderByDescending(c => c.NotificationCount)
                .ToList();

            // Top effective channels: channels with highest ActionRatePct per EventType
            var topChannels = rows
                .Where(r => r.Tier is EffectivenessTier.HighImpact or EffectivenessTier.Moderate)
                .GroupBy(r => r.ChannelType)
                .OrderByDescending(g => g.Average(r => r.ActionRatePct))
                .Select(g => g.Key)
                .Distinct()
                .Take(5)
                .ToList();

            // Health score: % EventTypes with Moderate or better
            var uniqueEventTypes = rows.Select(r => r.EventType).Distinct().Count();
            var effectiveEventTypes = rows
                .GroupBy(r => r.EventType)
                .Count(g => g.Any(r => r.Tier is EffectivenessTier.HighImpact or EffectivenessTier.Moderate));
            var healthScore = uniqueEventTypes == 0 ? 100m
                : Math.Round((decimal)effectiveEventTypes / uniqueEventTypes * 100m, 2);

            var adjustments = fatigueCandidates
                .Select(c => $"Reduce frequency for '{c.EventType}' (action rate {c.ActionRatePct:F1}%).")
                .ToList();

            return Result<Report>.Success(new Report(
                request.TenantId, periodStart, now,
                rows, fatigueCandidates, topChannels,
                healthScore, adjustments, now));
        }
    }
}
