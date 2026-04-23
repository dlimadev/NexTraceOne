namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Abstracção que fornece dados de efectividade de notificações (action rates, silence rates)
/// para o relatório GetNotificationEffectivenessReport.
/// Por omissão satisfeita por <c>NullNotificationEffectivenessReader</c>.
/// Wave AK.3 — GetNotificationEffectivenessReport.
/// </summary>
public interface INotificationEffectivenessReader
{
    Task<IReadOnlyList<EventTypeEffectivenessData>> GetEffectivenessDataAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        int actionWindowHours,
        CancellationToken cancellationToken = default);

    public sealed record EventTypeEffectivenessData(
        string EventType,
        string ChannelType,
        int NotificationCount,
        int ActionCount,
        int SilenceCount,
        decimal MedianTimeToActionMinutes);
}
