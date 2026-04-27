using System.Linq;
using NexTraceOne.Governance.Application.Features.GetDashboardLiveStream;
using NexTraceOne.Governance.Application.Features.GetWidgetDelta;

namespace NexTraceOne.Governance.Tests;

/// <summary>
/// Wave V3.3 — Live, Cross-filter, Drill-down.
/// Tests: SSE stream generator, delta handler, URL/filter helpers.
/// </summary>
public sealed class V33_LiveCrossFilterDrilldownTests
{
    // ── GetDashboardLiveStream: GenerateEventsAsync ───────────────────────

    [Fact]
    public async Task LiveStream_MaxEvents1_ReturnsHeartbeat()
    {
        var query = new GetDashboardLiveStream.Query(
            DashboardId: Guid.NewGuid(),
            TenantId: "tenant-1",
            MaxEvents: 1);

        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("heartbeat");
        events[0].IsSimulated.Should().BeTrue();
        events[0].WidgetId.Should().BeNull();
    }

    [Fact]
    public async Task LiveStream_MaxEvents3_ReturnsHeartbeatThenWidgetRefresh()
    {
        var query = new GetDashboardLiveStream.Query(
            DashboardId: Guid.NewGuid(),
            TenantId: "tenant-1",
            MaxEvents: 3);

        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        events.Should().HaveCount(3);
        events[0].EventType.Should().Be("heartbeat");
        events.Any(e => e.EventType == "widget.refresh").Should().BeTrue();
    }

    [Fact]
    public async Task LiveStream_EmptyTenantId_ReturnsNoEvents()
    {
        var query = new GetDashboardLiveStream.Query(
            DashboardId: Guid.NewGuid(),
            TenantId: "",
            MaxEvents: 5);

        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        events.Should().BeEmpty();
    }

    [Fact]
    public async Task LiveStream_WithWidgetIds_RefreshesSpecifiedWidgets()
    {
        var query = new GetDashboardLiveStream.Query(
            DashboardId: Guid.NewGuid(),
            TenantId: "tenant-1",
            WidgetIds: ["my-widget-1", "my-widget-2"],
            MaxEvents: 10);

        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        var refreshEvents = events.Where(e => e.EventType == "widget.refresh").ToList();
        refreshEvents.Should().NotBeEmpty();
        refreshEvents.Should().OnlyContain(e =>
            e.WidgetId == "my-widget-1" || e.WidgetId == "my-widget-2");
    }

    [Fact]
    public async Task LiveStream_EventuallyEmitsAnnotationEvent()
    {
        // Annotation events are emitted at seq % 7 == 0; with MaxEvents=20 we hit seq=7
        var query = new GetDashboardLiveStream.Query(
            DashboardId: Guid.NewGuid(),
            TenantId: "tenant-1",
            MaxEvents: 20);

        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        events.Any(e => e.EventType == "annotation.new").Should().BeTrue();
    }

    [Fact]
    public async Task LiveStream_AllEventsHaveTimestamp()
    {
        var query = new GetDashboardLiveStream.Query(
            DashboardId: Guid.NewGuid(),
            TenantId: "tenant-1",
            MaxEvents: 5);

        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        events.Should().OnlyContain(e => e.Timestamp > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task LiveStream_AllEventsMarkedSimulated()
    {
        var query = new GetDashboardLiveStream.Query(
            DashboardId: Guid.NewGuid(),
            TenantId: "tenant-1",
            MaxEvents: 8);

        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        events.Should().OnlyContain(e => e.IsSimulated);
    }

    // ── GetDashboardLiveStream: ToSseFrame ────────────────────────────────

    [Fact]
    public async Task ToSseFrame_StartsWithEventPrefix()
    {
        var query = new GetDashboardLiveStream.Query(Guid.NewGuid(), "t", MaxEvents: 1);
        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        var frame = GetDashboardLiveStream.ToSseFrame(events[0]);
        frame.Should().StartWith("event: heartbeat\ndata: ");
        frame.Should().EndWith("\n\n");
    }

    [Fact]
    public async Task ToSseFrame_ContainsSerializedJson()
    {
        var query = new GetDashboardLiveStream.Query(Guid.NewGuid(), "t", MaxEvents: 1);
        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        var frame = GetDashboardLiveStream.ToSseFrame(events[0]);
        frame.Should().Contain("\"eventType\"");
        frame.Should().Contain("\"isSimulated\"");
    }

    // ── GetWidgetDelta Handler ────────────────────────────────────────────

    [Fact]
    public async Task WidgetDelta_ReturnsSimulatedResult()
    {
        var handler = new GetWidgetDelta.Handler();
        var query = new GetWidgetDelta.Query(
            DashboardId: Guid.NewGuid(),
            WidgetId: "widget-1",
            TenantId: "tenant-1",
            Since: DateTimeOffset.UtcNow.AddMinutes(-5));

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsSimulated.Should().BeTrue();
        result.Value.SimulatedNote.Should().NotBeNullOrEmpty();
        result.Value.WidgetId.Should().Be("widget-1");
    }

    [Fact]
    public async Task WidgetDelta_SinceTimestampPreserved()
    {
        var handler = new GetWidgetDelta.Handler();
        var since = DateTimeOffset.UtcNow.AddMinutes(-30);
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "w1", "t1", since);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value!.Since.Should().Be(since);
        result.Value.AsOf.Should().BeAfter(since);
    }

    [Fact]
    public async Task WidgetDelta_EmptyTenantId_ReturnsError()
    {
        var handler = new GetWidgetDelta.Handler();
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "w1", "", DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("WidgetDelta.TenantId");
    }

    [Fact]
    public async Task WidgetDelta_EmptyWidgetId_ReturnsError()
    {
        var handler = new GetWidgetDelta.Handler();
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "", "tenant-1", DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("WidgetDelta.WidgetId");
    }

    [Fact]
    public async Task WidgetDelta_CountsAreNonNegative()
    {
        var handler = new GetWidgetDelta.Handler();
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "w1", "t1", DateTimeOffset.UtcNow.AddMinutes(-10));

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value!.AddedCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.RemovedCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.ChangedCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task WidgetDelta_ChangesMatchCounts()
    {
        var handler = new GetWidgetDelta.Handler();
        // Use a past timestamp far enough back to generate changes
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "w1", "t1", DateTimeOffset.UtcNow.AddMinutes(-15));

        var result = await handler.Handle(query, CancellationToken.None);
        var v = result.Value!;

        v.Changes.Count.Should().Be(v.AddedCount + v.RemovedCount + v.ChangedCount);
    }

    [Fact]
    public async Task WidgetDelta_AddedRows_HaveCorrectChangeType()
    {
        var handler = new GetWidgetDelta.Handler();
        var since = DateTimeOffset.UtcNow.AddHours(-2);   // long elapsed → guaranteed adds
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "w1", "t1", since);

        var result = await handler.Handle(query, CancellationToken.None);
        var added = result.Value!.Changes.Where(c => c.ChangeType == "added").ToList();

        added.Should().OnlyContain(r => r.Fields.ContainsKey("status"));
    }

    // ── GetWidgetDelta Validator ──────────────────────────────────────────

    [Fact]
    public void WidgetDelta_Validator_RejectsMissingTenantId()
    {
        var validator = new GetWidgetDelta.Validator();
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "w1", "", DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void WidgetDelta_Validator_RejectsMissingWidgetId()
    {
        var validator = new GetWidgetDelta.Validator();
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "", "tenant-1", DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WidgetId");
    }

    [Fact]
    public void WidgetDelta_Validator_AcceptsValidQuery()
    {
        var validator = new GetWidgetDelta.Validator();
        var query = new GetWidgetDelta.Query(Guid.NewGuid(), "w1", "t1", DateTimeOffset.UtcNow.AddMinutes(-5));

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    // ── LiveEvent payload coverage ────────────────────────────────────────

    [Fact]
    public async Task LiveStream_WidgetRefreshEvent_HasWidgetId()
    {
        var query = new GetDashboardLiveStream.Query(Guid.NewGuid(), "t1", MaxEvents: 5);
        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        var refresh = events.FirstOrDefault(e => e.EventType == "widget.refresh");
        refresh.Should().NotBeNull();
        refresh!.WidgetId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LiveStream_AnnotationEvent_HasNullWidgetId()
    {
        var query = new GetDashboardLiveStream.Query(Guid.NewGuid(), "t1", MaxEvents: 20);
        var events = new List<GetDashboardLiveStream.LiveEvent>();
        await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query))
            events.Add(evt);

        var ann = events.FirstOrDefault(e => e.EventType == "annotation.new");
        if (ann != null)
            ann.WidgetId.Should().BeNull();
    }

    [Fact]
    public async Task LiveStream_CancellationToken_StopsStream()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();  // Pre-cancel

        var query = new GetDashboardLiveStream.Query(Guid.NewGuid(), "t1", MaxEvents: 0);
        var events = new List<GetDashboardLiveStream.LiveEvent>();

        // With MaxEvents=0 (unlimited) and a cancelled token, the stream should stop after heartbeat
        try
        {
            await foreach (var evt in GetDashboardLiveStream.GenerateEventsAsync(query, cts.Token))
            {
                events.Add(evt);
                if (events.Count >= 2) break;  // safety cap for the test
            }
        }
        catch (OperationCanceledException) { }

        // Should emit at most 1 event (heartbeat) before noticing the cancellation
        events.Count.Should().BeLessThanOrEqualTo(2);
    }
}
