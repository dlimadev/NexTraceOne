using FluentAssertions;
using NSubstitute;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetEventProducerConsumerBalanceReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AH.2 — GetEventProducerConsumerBalanceReport.
/// Cobre OrphanedEvents, BlindConsumers, FanOutRisk, BalanceSummary, empty tenant e Validator.
/// </summary>
public sealed class WaveAhEventProducerConsumerBalanceReportTests
{
    private const string TenantId = "tenant-ah2";

    private static GetEventProducerConsumerBalanceReport.Handler CreateHandler(
        IReadOnlyList<EventBalanceEntry> entries)
    {
        var reader = Substitute.For<IEventProducerConsumerReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetEventProducerConsumerBalanceReport.Handler(reader);
    }

    private static EventBalanceEntry MakeEntry(
        string id,
        string name,
        int producers,
        int consumers,
        bool isActive = true) =>
        new(id, name, producers, consumers, isActive);

    private static GetEventProducerConsumerBalanceReport.Query DefaultQuery()
        => new(TenantId: TenantId, FanOutThreshold: 10);

    // ── Orphaned events ───────────────────────────────────────────────────

    [Fact]
    public async Task Identifies_orphaned_event_when_active_and_no_consumers()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-1", "order.archived", producers: 1, consumers: 0, isActive: true)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllEvents.Single().IsOrphaned.Should().BeTrue();
        result.Value.OrphanedEvents.Should().HaveCount(1);
        result.Value.OrphanedEvents.Single().EventName.Should().Be("order.archived");
    }

    [Fact]
    public async Task Does_not_flag_inactive_event_as_orphaned()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-2", "legacy.event", producers: 1, consumers: 0, isActive: false)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllEvents.Single().IsOrphaned.Should().BeFalse();
        result.Value.OrphanedEvents.Should().BeEmpty();
    }

    // ── Blind consumers ───────────────────────────────────────────────────

    [Fact]
    public async Task Identifies_blind_consumer_when_no_producer_but_has_consumers()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-3", "external.import", producers: 0, consumers: 3)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllEvents.Single().IsBlind.Should().BeTrue();
        result.Value.BlindConsumers.Should().HaveCount(1);
    }

    [Fact]
    public async Task Does_not_flag_event_as_blind_when_has_producer()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-4", "payment.confirmed", producers: 1, consumers: 2)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllEvents.Single().IsBlind.Should().BeFalse();
        result.Value.BlindConsumers.Should().BeEmpty();
    }

    // ── FanOut risk ───────────────────────────────────────────────────────

    [Fact]
    public async Task Identifies_fan_out_risk_when_consumer_count_meets_threshold()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-5", "product.published", producers: 1, consumers: 10)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery() with { FanOutThreshold = 10 }, CancellationToken.None);

        result.Value.AllEvents.Single().FanOutRisk.Should().BeTrue();
        result.Value.HighFanOutEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Does_not_flag_fan_out_when_below_threshold()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-6", "user.created", producers: 1, consumers: 5)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery() with { FanOutThreshold = 10 }, CancellationToken.None);

        result.Value.AllEvents.Single().FanOutRisk.Should().BeFalse();
        result.Value.HighFanOutEvents.Should().BeEmpty();
    }

    // ── HighFanOutEvents ordering ─────────────────────────────────────────

    [Fact]
    public async Task HighFanOutEvents_ordered_by_consumer_count_desc()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-7", "evt-a", producers: 1, consumers: 15),
            MakeEntry("c-8", "evt-b", producers: 1, consumers: 25),
            MakeEntry("c-9", "evt-c", producers: 1, consumers: 12),
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery() with { FanOutThreshold = 10, TopFanOutCount = 3 }, CancellationToken.None);

        result.Value.HighFanOutEvents[0].ContractId.Should().Be("c-8"); // 25
        result.Value.HighFanOutEvents[1].ContractId.Should().Be("c-7"); // 15
        result.Value.HighFanOutEvents[2].ContractId.Should().Be("c-9"); // 12
    }

    // ── BalanceSummary ────────────────────────────────────────────────────

    [Fact]
    public async Task BalanceSummary_calculates_percentages_correctly()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-10", "orphaned", producers: 1, consumers: 0, isActive: true),
            MakeEntry("c-11", "blind", producers: 0, consumers: 2),
            MakeEntry("c-12", "fanout", producers: 1, consumers: 10),
            MakeEntry("c-13", "normal", producers: 1, consumers: 2),
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery() with { FanOutThreshold = 10 }, CancellationToken.None);

        var summary = result.Value.Summary;
        summary.TotalEvents.Should().Be(4);
        summary.OrphanedCount.Should().Be(1);
        summary.BlindConsumerCount.Should().Be(1);
        summary.FanOutRiskCount.Should().Be(1);
        summary.OrphanedPct.Should().Be(25.0);
        summary.BlindConsumerPct.Should().Be(25.0);
        summary.FanOutRiskPct.Should().Be(25.0);
    }

    // ── Empty tenant ──────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_empty_report_when_no_events()
    {
        var handler = CreateHandler(Array.Empty<EventBalanceEntry>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllEvents.Should().BeEmpty();
        result.Value.OrphanedEvents.Should().BeEmpty();
        result.Value.BlindConsumers.Should().BeEmpty();
        result.Value.HighFanOutEvents.Should().BeEmpty();
        result.Value.Summary.TotalEvents.Should().Be(0);
    }

    // ── Event with both producer and consumers (healthy) ──────────────────

    [Fact]
    public async Task Normal_event_has_no_flags_set()
    {
        var entries = new List<EventBalanceEntry>
        {
            MakeEntry("c-14", "catalog.updated", producers: 2, consumers: 3)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var entry = result.Value.AllEvents.Single();
        entry.IsOrphaned.Should().BeFalse();
        entry.IsBlind.Should().BeFalse();
        entry.FanOutRisk.Should().BeFalse();
    }

    // ── Null reader ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_throws_when_reader_is_null()
    {
        var act = () => new GetEventProducerConsumerBalanceReport.Handler(null!);
        act.Should().Throw<Exception>();
    }
}
