using System.Linq;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetReleaseHealthTimeline;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes de unidade para o handler GetReleaseHealthTimeline.
/// Verifica filtragem de snapshots por janela de tempo e montagem da timeline.
/// </summary>
public sealed class GetReleaseHealthTimelineTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 4, 19, 8, 0, 0, TimeSpan.Zero);

    private static RuntimeSnapshot MakeSnapshot(DateTimeOffset capturedAt, decimal errorRate = 0m)
        => RuntimeSnapshot.Create(
            "OrderService", "production",
            avgLatencyMs: 50m,
            p99LatencyMs: 120m,
            errorRate: errorRate,
            requestsPerSecond: 200m,
            cpuUsagePercent: 30m,
            memoryUsageMb: 512m,
            activeInstances: 3,
            capturedAt: capturedAt,
            source: "OtelCollector");

    [Fact]
    public async Task GetReleaseHealthTimeline_Should_ReturnOnlySnapshotsInWindow()
    {
        // Three snapshots — only two fall within the window
        var inWindow1 = MakeSnapshot(BaseTime.AddMinutes(10));
        var inWindow2 = MakeSnapshot(BaseTime.AddMinutes(20));
        var outsideWindow = MakeSnapshot(BaseTime.AddHours(3));

        var repo = Substitute.For<IRuntimeSnapshotRepository>();
        repo.ListByServiceAsync("OrderService", "production", 1, 1000, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<RuntimeSnapshot>)new List<RuntimeSnapshot>
            {
                inWindow1, inWindow2, outsideWindow
            });

        var handler = new GetReleaseHealthTimeline.Handler(repo);
        var result = await handler.Handle(
            new GetReleaseHealthTimeline.Query(
                "OrderService", "production",
                WindowStart: BaseTime,
                WindowEnd: BaseTime.AddHours(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("OrderService");
        result.Value.Environment.Should().Be("production");
        result.Value.DataPointCount.Should().Be(2);
        result.Value.Timeline.Should().HaveCount(2);
        result.Value.Timeline.Select(t => t.CapturedAt).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetReleaseHealthTimeline_Should_ReturnEmpty_When_NoSnapshotsInWindow()
    {
        var repo = Substitute.For<IRuntimeSnapshotRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<RuntimeSnapshot>)new List<RuntimeSnapshot>());

        var handler = new GetReleaseHealthTimeline.Handler(repo);
        var result = await handler.Handle(
            new GetReleaseHealthTimeline.Query(
                "NewSvc", "production",
                WindowStart: BaseTime,
                WindowEnd: BaseTime.AddHours(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DataPointCount.Should().Be(0);
        result.Value.Timeline.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReleaseHealthTimeline_Should_IncludeHealthStatus_In_EachItem()
    {
        var snapshot = MakeSnapshot(BaseTime.AddMinutes(5), errorRate: 0.5m);

        var repo = Substitute.For<IRuntimeSnapshotRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<RuntimeSnapshot>)new List<RuntimeSnapshot> { snapshot });

        var handler = new GetReleaseHealthTimeline.Handler(repo);
        var result = await handler.Handle(
            new GetReleaseHealthTimeline.Query(
                "OrderService", "production",
                WindowStart: BaseTime,
                WindowEnd: BaseTime.AddHours(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Timeline.Single();
        item.HealthStatus.Should().NotBeNullOrEmpty();
        item.ErrorRate.Should().Be(0.5m);
        item.AvgLatencyMs.Should().Be(50m);
    }

    [Fact]
    public void GetReleaseHealthTimeline_Validator_Should_Reject_WhenWindowEndBeforeStart()
    {
        var validator = new GetReleaseHealthTimeline.Validator();
        var result = validator.Validate(new GetReleaseHealthTimeline.Query(
            "Svc", "prod",
            WindowStart: BaseTime.AddHours(1),
            WindowEnd: BaseTime));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetReleaseHealthTimeline_Validator_Should_Accept_ValidQuery()
    {
        var validator = new GetReleaseHealthTimeline.Validator();
        var result = validator.Validate(new GetReleaseHealthTimeline.Query(
            "Svc", "prod",
            WindowStart: BaseTime,
            WindowEnd: BaseTime.AddHours(2)));
        result.IsValid.Should().BeTrue();
    }
}
