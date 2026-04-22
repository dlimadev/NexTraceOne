using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Application.Features.GetNotificationDeliveryReport;

/// <summary>
/// Feature: GetNotificationDeliveryReport — relatório de saúde e volume de entrega de notificações por canal.
///
/// Calcula:
/// - DeliverySuccessRate: % entregas bem-sucedidas por canal no período
/// - ChannelHealthTier: Healthy ≥99% / Degraded ≥95% / Failing &lt;95%
/// - EventTypeDistribution: tipos de evento mais frequentes
/// - DeadLetterCount: mensagens não entregues após max retries
/// - TopRecipients: utilizadores/equipas com mais notificações no período
///
/// Wave AK.2 — Developer Experience &amp; Notification Management (Notifications).
/// </summary>
public static class GetNotificationDeliveryReport
{
    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal HealthyThreshold = 99m;
    private const decimal DegradedThreshold = 95m;

    internal const int DefaultLookbackDays = 30;
    internal const int TopRecipientsCount = 10;

    // ── Query ──────────────────────────────────────────────────────────────

    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    public enum ChannelHealthTier { Healthy, Degraded, Failing }

    // ── Response records ───────────────────────────────────────────────────

    public sealed record ChannelDeliverySummary(
        string ChannelType,
        int TotalAttempts,
        int SuccessCount,
        int FailureCount,
        int DeadLetterCount,
        decimal DeliverySuccessRate,
        ChannelHealthTier HealthTier);

    public sealed record EventTypeVolume(
        string EventType,
        int NotificationCount,
        decimal PctOfTotal);

    public sealed record RecipientVolume(
        string RecipientId,
        string RecipientType,
        int NotificationCount);

    public sealed record Report(
        string TenantId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        IReadOnlyList<ChannelDeliverySummary> ByChannel,
        IReadOnlyList<EventTypeVolume> EventTypeDistribution,
        IReadOnlyList<RecipientVolume> TopRecipients,
        int TotalDeadLetterCount,
        decimal OverallDeliverySuccessRate,
        ChannelHealthTier OverallHealthTier,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        INotificationDeliveryReportReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var periodStart = now.AddDays(-request.LookbackDays);

            var data = await reader.GetDeliveryDataAsync(request.TenantId, periodStart, now, cancellationToken);

            var byChannel = data.ChannelAttempts
                .Select(c =>
                {
                    var total = c.SuccessCount + c.FailureCount;
                    var rate = total == 0 ? 100m : Math.Round((decimal)c.SuccessCount / total * 100m, 2);
                    var tier = rate >= HealthyThreshold ? ChannelHealthTier.Healthy
                        : rate >= DegradedThreshold ? ChannelHealthTier.Degraded
                        : ChannelHealthTier.Failing;
                    return new ChannelDeliverySummary(
                        c.ChannelType, total, c.SuccessCount, c.FailureCount,
                        c.DeadLetterCount, rate, tier);
                })
                .OrderByDescending(c => c.TotalAttempts)
                .ToList();

            var totalAttempts = byChannel.Sum(c => c.TotalAttempts);
            var totalSuccess = byChannel.Sum(c => c.SuccessCount);
            var overallRate = totalAttempts == 0 ? 100m
                : Math.Round((decimal)totalSuccess / totalAttempts * 100m, 2);
            var overallTier = overallRate >= HealthyThreshold ? ChannelHealthTier.Healthy
                : overallRate >= DegradedThreshold ? ChannelHealthTier.Degraded
                : ChannelHealthTier.Failing;

            var eventDist = data.EventTypeCounts
                .Select(e => new EventTypeVolume(
                    e.EventType,
                    e.Count,
                    totalAttempts == 0 ? 0m : Math.Round((decimal)e.Count / totalAttempts * 100m, 2)))
                .OrderByDescending(e => e.NotificationCount)
                .ToList();

            var topRecipients = data.RecipientCounts
                .OrderByDescending(r => r.Count)
                .Take(TopRecipientsCount)
                .Select(r => new RecipientVolume(r.RecipientId, r.RecipientType, r.Count))
                .ToList();

            var totalDead = byChannel.Sum(c => c.DeadLetterCount);

            return Result<Report>.Success(new Report(
                request.TenantId, periodStart, now,
                byChannel, eventDist, topRecipients,
                totalDead, overallRate, overallTier, now));
        }
    }
}
