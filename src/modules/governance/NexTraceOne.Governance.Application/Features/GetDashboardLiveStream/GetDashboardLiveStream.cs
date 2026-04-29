using System.Runtime.CompilerServices;
using System.Text.Json;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetDashboardLiveStream;

/// <summary>
/// Feature: GetDashboardLiveStream — canal SSE de eventos ao vivo para um dashboard.
///
/// Quando um IDashboardDataBridge está registado, emite widget.refresh com IsSimulated=false.
/// Sem bridge (NullDashboardDataBridge), emite heartbeats com IsSimulated=true.
///
/// Wave V3.3 — Live, Cross-filter, Drill-down.
/// </summary>
public static class GetDashboardLiveStream
{
    // ── Types ─────────────────────────────────────────────────────────────

    public sealed record Query(
        Guid DashboardId,
        string TenantId,
        IReadOnlyList<string>? WidgetIds = null,
        int MaxEvents = 0);   // 0 = unlimited (HTTP); set in tests to cap

    public sealed record LiveEvent(
        string EventType,
        string? WidgetId,
        DateTimeOffset Timestamp,
        bool IsSimulated,
        object Payload);

    // ── SSE stream generator ──────────────────────────────────────────────

    /// <summary>
    /// Produces a stream of live events. Terminates when <paramref name="ct"/> is cancelled
    /// or <see cref="Query.MaxEvents"/> is reached (when > 0).
    /// </summary>
    public static async IAsyncEnumerable<LiveEvent> GenerateEventsAsync(
        Query query,
        IDashboardDataBridge? bridge = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.TenantId))
            yield break;

        var effectiveBridge = bridge ?? NullDashboardDataBridge.Instance;
        var hasRealBridge = bridge is not null and not NullDashboardDataBridge;

        var emitted = 0;
        var seq = 0;
        var lastPoll = DateTimeOffset.UtcNow;

        // Initial heartbeat so the client knows the channel is alive
        yield return new LiveEvent(
            EventType: "heartbeat",
            WidgetId: null,
            Timestamp: DateTimeOffset.UtcNow,
            IsSimulated: !hasRealBridge,
            Payload: new { seq = seq++, note = hasRealBridge ? "Live channel open" : "Live channel open (no real-time bridge configured)" });

        emitted++;
        if (query.MaxEvents > 0 && emitted >= query.MaxEvents) yield break;

        while (!ct.IsCancellationRequested)
        {
            // Poll the bridge for real events since last tick
            var pendingEvents = await effectiveBridge.GetPendingEventsAsync(
                query.DashboardId, query.TenantId, query.WidgetIds, lastPoll, ct);

            lastPoll = DateTimeOffset.UtcNow;

            if (pendingEvents.Count > 0)
            {
                foreach (var evt in pendingEvents)
                {
                    yield return evt;
                    emitted++;
                    if (query.MaxEvents > 0 && emitted >= query.MaxEvents) yield break;
                }
            }
            else
            {
                // No real events — emit heartbeat (honest-gap: IsSimulated when no bridge)
                if (seq % 3 == 0 || !hasRealBridge)
                {
                    yield return new LiveEvent(
                        EventType: "heartbeat",
                        WidgetId: null,
                        Timestamp: DateTimeOffset.UtcNow,
                        IsSimulated: !hasRealBridge,
                        Payload: new { seq, connected = true });

                    emitted++;
                    if (query.MaxEvents > 0 && emitted >= query.MaxEvents) yield break;
                }
            }

            seq++;

            if (query.MaxEvents == 0)
                await Task.Delay(5_000, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Serializes a <see cref="LiveEvent"/> to the SSE wire format.</summary>
    public static string ToSseFrame(LiveEvent evt)
    {
        var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        return $"event: {evt.EventType}\ndata: {json}\n\n";
    }
}
