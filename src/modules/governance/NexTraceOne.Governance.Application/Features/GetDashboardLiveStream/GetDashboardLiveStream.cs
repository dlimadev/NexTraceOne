using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NexTraceOne.Governance.Application.Features.GetDashboardLiveStream;

/// <summary>
/// Feature: GetDashboardLiveStream — canal SSE de eventos ao vivo para um dashboard.
///
/// Gera um stream de eventos simulados (widget.refresh, annotation.new, heartbeat)
/// para o canal SSE em /governance/dashboards/{id}/live.
///
/// As atualizações reais de widgets requerem bridge com fontes de dados externas
/// (honest-gap: IsSimulated=true até que os bridges estejam ligados).
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
        int MaxEvents = 0);   // 0 = unlimited (for HTTP); set in tests to cap

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
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.TenantId))
            yield break;

        var widgetIds = query.WidgetIds?.Count > 0
            ? query.WidgetIds
            : (IReadOnlyList<string>)["widget-1", "widget-2", "widget-3"];

        var emitted = 0;
        var seq = 0;

        // Emit an initial heartbeat immediately so the client knows the channel is alive
        yield return new LiveEvent(
            EventType: "heartbeat",
            WidgetId: null,
            Timestamp: DateTimeOffset.UtcNow,
            IsSimulated: true,
            Payload: new { seq = seq++, note = "Live channel open (simulated)" });

        emitted++;
        if (query.MaxEvents > 0 && emitted >= query.MaxEvents) yield break;

        while (!ct.IsCancellationRequested)
        {
            // Heartbeat every ~15 ticks (simulated as 0 ms in tests via MaxEvents)
            if (seq % 15 == 0)
            {
                yield return new LiveEvent("heartbeat", null, DateTimeOffset.UtcNow, true,
                    new { seq, connected = true });
                emitted++;
                if (query.MaxEvents > 0 && emitted >= query.MaxEvents) yield break;
            }

            // Widget refresh event
            var widgetId = widgetIds[seq % widgetIds.Count];
            yield return new LiveEvent(
                EventType: "widget.refresh",
                WidgetId: widgetId,
                Timestamp: DateTimeOffset.UtcNow,
                IsSimulated: true,
                Payload: BuildWidgetRefreshPayload(widgetId, seq));

            emitted++;
            if (query.MaxEvents > 0 && emitted >= query.MaxEvents) yield break;

            // Occasional annotation event
            if (seq % 7 == 0)
            {
                yield return new LiveEvent(
                    EventType: "annotation.new",
                    WidgetId: null,
                    Timestamp: DateTimeOffset.UtcNow,
                    IsSimulated: true,
                    Payload: new
                    {
                        id = $"ann-{seq}",
                        type = seq % 2 == 0 ? "change.deploy" : "incident.opened",
                        title = seq % 2 == 0 ? "Simulated deploy detected" : "Simulated incident opened",
                        severity = "info",
                    });

                emitted++;
                if (query.MaxEvents > 0 && emitted >= query.MaxEvents) yield break;
            }

            seq++;

            // In real operation, wait between events; in tests MaxEvents terminates the loop
            if (query.MaxEvents == 0)
                await Task.Delay(5_000, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Serializes a <see cref="LiveEvent"/> to the SSE wire format.
    /// </summary>
    public static string ToSseFrame(LiveEvent evt)
    {
        var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        return $"event: {evt.EventType}\ndata: {json}\n\n";
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static object BuildWidgetRefreshPayload(string widgetId, int seq)
    {
        return new
        {
            widgetId,
            seq,
            dataPoints = new[]
            {
                new { label = "p50",  value = 120 + (seq % 40) },
                new { label = "p95",  value = 280 + (seq % 80) },
                new { label = "p99",  value = 480 + (seq % 120) },
            },
            note = "Simulated live data — connect real-time ingestion for live updates.",
        };
    }
}
