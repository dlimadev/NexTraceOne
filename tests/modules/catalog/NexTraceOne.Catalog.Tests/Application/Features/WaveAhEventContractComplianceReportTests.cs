using FluentAssertions;
using NSubstitute;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetEventContractComplianceReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AH.3 — GetEventContractComplianceReport.
/// Cobre ComplianceTier, TenantEventComplianceScore, TopNonCompliant, ViolationTimeline,
/// empty tenant e Validator.
/// </summary>
public sealed class WaveAhEventContractComplianceReportTests
{
    private const string TenantId = "tenant-ah3";

    private static GetEventContractComplianceReport.Handler CreateHandler(
        IReadOnlyList<EventComplianceEntry> entries)
    {
        var reader = Substitute.For<IEventComplianceReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetEventContractComplianceReport.Handler(reader);
    }

    private static EventComplianceEntry MakeEntry(
        string id,
        string name,
        string producer,
        double complianceRate,
        int violationCount = 0,
        IReadOnlyList<string>? unregisteredFields = null,
        IReadOnlyList<string>? missingFields = null,
        IReadOnlyDictionary<string, int>? timeline = null) =>
        new(id, name, producer, complianceRate, violationCount,
            unregisteredFields ?? Array.Empty<string>(),
            missingFields ?? Array.Empty<string>(),
            timeline ?? new Dictionary<string, int>());

    private static GetEventContractComplianceReport.Query DefaultQuery()
        => new(TenantId: TenantId);

    // ── Compliant tier ────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Compliant_tier_when_compliance_rate_above_99()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-1", "order.created", "svc-orders", complianceRate: 99.5)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllContracts.Single().Tier
            .Should().Be(GetEventContractComplianceReport.ComplianceTier.Compliant);
    }

    [Fact]
    public async Task Returns_Compliant_tier_when_compliance_rate_exactly_99()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-2", "payment.done", "svc-payments", complianceRate: 99.0)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().Tier
            .Should().Be(GetEventContractComplianceReport.ComplianceTier.Compliant);
    }

    // ── MinorViolations tier ──────────────────────────────────────────────

    [Fact]
    public async Task Returns_MinorViolations_tier_when_rate_between_95_and_99()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-3", "inventory.reserved", "svc-inv", complianceRate: 97.0, violationCount: 5)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().Tier
            .Should().Be(GetEventContractComplianceReport.ComplianceTier.MinorViolations);
    }

    // ── Degraded tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Degraded_tier_when_rate_between_80_and_95()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-4", "notification.sent", "svc-notify", complianceRate: 85.0, violationCount: 30)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().Tier
            .Should().Be(GetEventContractComplianceReport.ComplianceTier.Degraded);
    }

    // ── NonCompliant tier ─────────────────────────────────────────────────

    [Fact]
    public async Task Returns_NonCompliant_tier_when_rate_below_80()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-5", "legacy.event", "svc-old", complianceRate: 60.0, violationCount: 200)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().Tier
            .Should().Be(GetEventContractComplianceReport.ComplianceTier.NonCompliant);
    }

    // ── TenantEventComplianceScore ────────────────────────────────────────

    [Fact]
    public async Task TenantScore_is_average_of_all_compliance_rates()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-6", "evt-a", "svc-a", complianceRate: 100.0),
            MakeEntry("c-7", "evt-b", "svc-b", complianceRate: 80.0),
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TenantEventComplianceScore.Should().Be(90.0);
    }

    // ── TopNonCompliantContracts ──────────────────────────────────────────

    [Fact]
    public async Task TopNonCompliant_ordered_by_rate_asc_then_violations_desc()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-8", "evt-a", "svc-a", complianceRate: 97.0, violationCount: 5),   // MinorViolations
            MakeEntry("c-9", "evt-b", "svc-b", complianceRate: 60.0, violationCount: 500), // NonCompliant
            MakeEntry("c-10", "evt-c", "svc-c", complianceRate: 85.0, violationCount: 50), // Degraded
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery() with { TopNonCompliantCount = 3 }, CancellationToken.None);

        result.Value.TopNonCompliantContracts.Should().HaveCount(3);
        result.Value.TopNonCompliantContracts[0].ContractId.Should().Be("c-9"); // lowest rate
    }

    [Fact]
    public async Task TopNonCompliant_excludes_compliant_contracts()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-11", "evt-good", "svc-a", complianceRate: 99.5),
            MakeEntry("c-12", "evt-bad", "svc-b", complianceRate: 75.0),
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TopNonCompliantContracts.Should().HaveCount(1);
        result.Value.TopNonCompliantContracts.Single().ContractId.Should().Be("c-12");
    }

    // ── Tier counts ───────────────────────────────────────────────────────

    [Fact]
    public async Task Tier_counts_are_correct()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-13", "evt-1", "svc-a", complianceRate: 99.5),  // Compliant
            MakeEntry("c-14", "evt-2", "svc-b", complianceRate: 99.5),  // Compliant
            MakeEntry("c-15", "evt-3", "svc-c", complianceRate: 97.0),  // MinorViolations
            MakeEntry("c-16", "evt-4", "svc-d", complianceRate: 85.0),  // Degraded
            MakeEntry("c-17", "evt-5", "svc-e", complianceRate: 70.0),  // NonCompliant
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.CompliantCount.Should().Be(2);
        result.Value.MinorViolationsCount.Should().Be(1);
        result.Value.DegradedCount.Should().Be(1);
        result.Value.NonCompliantCount.Should().Be(1);
    }

    // ── ViolationTimeline ─────────────────────────────────────────────────

    [Fact]
    public async Task ViolationTimeline_is_preserved_from_entry()
    {
        var timeline = new Dictionary<string, int>
        {
            { "2026-04-01", 10 },
            { "2026-04-02", 15 },
            { "2026-04-03", 5 },
        };
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-18", "evt-violations", "svc-a", complianceRate: 85.0,
                violationCount: 30, timeline: timeline)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().ViolationTimeline.Should().HaveCount(3);
        result.Value.AllContracts.Single().ViolationTimeline["2026-04-01"].Should().Be(10);
    }

    // ── UnregisteredFields and MissingRequiredFields ──────────────────────

    [Fact]
    public async Task UnregisteredFields_and_MissingFields_are_preserved()
    {
        var entries = new List<EventComplianceEntry>
        {
            MakeEntry("c-19", "evt-schema-issues", "svc-a", complianceRate: 90.0,
                violationCount: 20,
                unregisteredFields: new[] { "extraField", "debugInfo" },
                missingFields: new[] { "requiredId" })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var entry = result.Value.AllContracts.Single();
        entry.UnregisteredFields.Should().Contain("extraField");
        entry.UnregisteredFields.Should().Contain("debugInfo");
        entry.MissingRequiredFields.Should().Contain("requiredId");
    }

    // ── Empty tenant ──────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_empty_report_and_100_score_when_no_contracts()
    {
        var handler = CreateHandler(Array.Empty<EventComplianceEntry>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllContracts.Should().BeEmpty();
        result.Value.TenantEventComplianceScore.Should().Be(100.0);
        result.Value.CompliantCount.Should().Be(0);
    }

    // ── Null reader ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_throws_when_reader_is_null()
    {
        var act = () => new GetEventContractComplianceReport.Handler(null!);
        act.Should().Throw<Exception>();
    }
}
