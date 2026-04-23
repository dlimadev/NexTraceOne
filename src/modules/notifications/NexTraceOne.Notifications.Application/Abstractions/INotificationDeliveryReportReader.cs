namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Abstracção que fornece dados agregados de entrega de notificações para análise de saúde por canal.
/// Por omissão é satisfeita por <c>NullNotificationDeliveryReportReader</c>.
/// Wave AK.2 — GetNotificationDeliveryReport.
/// </summary>
public interface INotificationDeliveryReportReader
{
    Task<DeliveryReportData> GetDeliveryDataAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    public sealed record ChannelAttemptData(
        string ChannelType,
        int SuccessCount,
        int FailureCount,
        int DeadLetterCount);

    public sealed record EventTypeCountData(
        string EventType,
        int Count);

    public sealed record RecipientCountData(
        string RecipientId,
        string RecipientType,
        int Count);

    public sealed record DeliveryReportData(
        IReadOnlyList<ChannelAttemptData> ChannelAttempts,
        IReadOnlyList<EventTypeCountData> EventTypeCounts,
        IReadOnlyList<RecipientCountData> RecipientCounts);
}
