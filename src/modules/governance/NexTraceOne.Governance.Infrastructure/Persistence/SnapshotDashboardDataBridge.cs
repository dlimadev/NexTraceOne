using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetDashboardLiveStream;

namespace NexTraceOne.Governance.Infrastructure.Persistence;

/// <summary>
/// Bridge real que detecta mudanças de widgets via snapshots persistidos.
/// Emite widget.refresh com IsSimulated=false para cada snapshot novo desde <c>since</c>.
/// </summary>
internal sealed class SnapshotDashboardDataBridge(IWidgetSnapshotRepository snapshots)
    : IDashboardDataBridge
{
    public async Task<IReadOnlyList<GetDashboardLiveStream.LiveEvent>> GetPendingEventsAsync(
        Guid dashboardId,
        string tenantId,
        IReadOnlyList<string>? widgetIds,
        DateTimeOffset since,
        CancellationToken ct)
    {
        var events = new List<GetDashboardLiveStream.LiveEvent>();
        var effectiveWidgetIds = widgetIds?.Count > 0 ? widgetIds : null;

        if (effectiveWidgetIds is not null)
        {
            foreach (var wid in effectiveWidgetIds)
            {
                var recent = await snapshots.ListSinceAsync(tenantId, dashboardId, wid, since, ct);
                foreach (var snap in recent)
                    events.Add(ToRefreshEvent(wid, snap.DataJson, snap.CapturedAt));
            }
        }
        else
        {
            // widgetIds unknown — fetch any snapshot for this dashboard/tenant since last tick
            var recent = await snapshots.ListSinceAsync(tenantId, dashboardId, string.Empty, since, ct);
            foreach (var snap in recent)
                events.Add(ToRefreshEvent(snap.WidgetId, snap.DataJson, snap.CapturedAt));
        }

        return events;
    }

    private static GetDashboardLiveStream.LiveEvent ToRefreshEvent(
        string widgetId, string dataJson, DateTimeOffset capturedAt)
        => new(
            EventType: "widget.refresh",
            WidgetId: widgetId,
            Timestamp: capturedAt,
            IsSimulated: false,
            Payload: new { widgetId, dataJson, source = "snapshot" });
}
