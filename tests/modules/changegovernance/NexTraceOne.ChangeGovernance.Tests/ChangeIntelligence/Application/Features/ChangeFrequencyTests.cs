using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeFrequencyHeatmap;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentCadenceReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using DeploymentCadence = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentCadenceReport.GetDeploymentCadenceReport.DeploymentCadence;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave K.3 — Change Frequency Analytics.
/// Cobre GetChangeFrequencyHeatmap e GetDeploymentCadenceReport.
/// </summary>
public sealed class ChangeFrequencyTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ICurrentTenant CreateTenant()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(TenantId);
        return tenant;
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(
        string serviceName = "payment-service",
        DateTimeOffset? createdAt = null)
    {
        var at = createdAt ?? FixedNow.AddDays(-1);
        return Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0", "production", "jenkins", "abc123", at);
    }

    // ── GetChangeFrequencyHeatmap tests ─────────────────────────────────────

    [Fact]
    public async Task GetChangeFrequencyHeatmap_Returns_Empty_When_No_Releases()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);

        var handler = new GetChangeFrequencyHeatmap.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetChangeFrequencyHeatmap.Query(30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDeploys.Should().Be(0);
        result.Value.Heatmap.Should().BeEmpty();
        result.Value.MaxCellCount.Should().Be(0);
        result.Value.PeakDayOfWeek.Should().BeNull();
        result.Value.PeakHourOfDay.Should().BeNull();
    }

    [Fact]
    public async Task GetChangeFrequencyHeatmap_Groups_By_DayOfWeek_And_Hour()
    {
        // Two releases on the same day+hour
        var at1 = new DateTimeOffset(2026, 4, 20, 14, 0, 0, TimeSpan.Zero); // Monday 14h UTC
        var at2 = new DateTimeOffset(2026, 4, 20, 14, 30, 0, TimeSpan.Zero); // Monday 14h UTC (same cell)
        var at3 = new DateTimeOffset(2026, 4, 21, 10, 0, 0, TimeSpan.Zero); // Tuesday 10h UTC

        var r1 = MakeRelease(createdAt: at1);
        var r2 = MakeRelease(createdAt: at2);
        var r3 = MakeRelease(createdAt: at3);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[r1, r2, r3]);

        var handler = new GetChangeFrequencyHeatmap.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetChangeFrequencyHeatmap.Query(30), CancellationToken.None);

        result.Value.TotalDeploys.Should().Be(3);
        result.Value.Heatmap.Should().HaveCount(2);
        result.Value.MaxCellCount.Should().Be(2);
    }

    [Fact]
    public async Task GetChangeFrequencyHeatmap_Peak_Cell_Is_Correctly_Identified()
    {
        var at1 = new DateTimeOffset(2026, 4, 18, 16, 0, 0, TimeSpan.Zero); // Saturday 16h
        var at2 = new DateTimeOffset(2026, 4, 18, 16, 0, 0, TimeSpan.Zero); // Saturday 16h
        var at3 = new DateTimeOffset(2026, 4, 19, 10, 0, 0, TimeSpan.Zero); // Sunday 10h

        var r1 = MakeRelease(createdAt: at1);
        var r2 = MakeRelease(createdAt: at2);
        var r3 = MakeRelease(createdAt: at3);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[r1, r2, r3]);

        var handler = new GetChangeFrequencyHeatmap.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetChangeFrequencyHeatmap.Query(30), CancellationToken.None);

        result.Value.PeakDayOfWeek.Should().Be((int)DayOfWeek.Saturday);
        result.Value.PeakHourOfDay.Should().Be(16);
    }

    [Fact]
    public async Task GetChangeFrequencyHeatmap_ByDayOfWeek_Has_7_Entries()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);

        var handler = new GetChangeFrequencyHeatmap.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetChangeFrequencyHeatmap.Query(30), CancellationToken.None);

        result.Value.ByDayOfWeek.Should().HaveCount(7);
        result.Value.ByDayOfWeek.Select(d => d.DayOfWeek).Should().BeEquivalentTo([0, 1, 2, 3, 4, 5, 6]);
    }

    [Fact]
    public void GetChangeFrequencyHeatmap_Validator_Rejects_Days_Too_Low()
    {
        var validator = new GetChangeFrequencyHeatmap.Validator();
        var result = validator.Validate(new GetChangeFrequencyHeatmap.Query(6));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetChangeFrequencyHeatmap_Validator_Rejects_Days_Too_High()
    {
        var validator = new GetChangeFrequencyHeatmap.Validator();
        var result = validator.Validate(new GetChangeFrequencyHeatmap.Query(91));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetChangeFrequencyHeatmap_Validator_Accepts_Valid_Query()
    {
        var validator = new GetChangeFrequencyHeatmap.Validator();
        var result = validator.Validate(new GetChangeFrequencyHeatmap.Query(30));
        result.IsValid.Should().BeTrue();
    }

    // ── GetDeploymentCadenceReport tests ─────────────────────────────────────

    [Fact]
    public async Task GetDeploymentCadenceReport_Returns_Empty_When_No_Releases()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);

        var handler = new GetDeploymentCadenceReport.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetDeploymentCadenceReport.Query(30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServices.Should().Be(0);
        result.Value.TotalDeploys.Should().Be(0);
        result.Value.OverallCadence.Should().Be(DeploymentCadence.Insufficient);
        result.Value.Services.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeploymentCadenceReport_HighPerformer_When_Over_1_Per_Day()
    {
        // 30 releases in 30 days = 1/day = HighPerformer
        var releases = Enumerable.Range(0, 30)
            .Select(i => MakeRelease(createdAt: FixedNow.AddDays(-i)))
            .ToList();

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)releases);

        var handler = new GetDeploymentCadenceReport.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetDeploymentCadenceReport.Query(30), CancellationToken.None);

        result.Value.Services.Should().HaveCount(1);
        result.Value.Services[0].Cadence.Should().Be(DeploymentCadence.HighPerformer);
    }

    [Fact]
    public async Task GetDeploymentCadenceReport_Medium_When_About_1_Per_Week()
    {
        // 4 releases in 30 days ~ 0.13/day = Medium (>1/week = 0.143/day)
        // Actually 4/30 = 0.133 which is < 1/7 = 0.143, so it's LowPerformer
        // Let's use 5 releases in 30 days = 0.167/day > 1/7 = Medium
        var releases = Enumerable.Range(0, 5)
            .Select(i => MakeRelease(createdAt: FixedNow.AddDays(-i * 5)))
            .ToList();

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)releases);

        var handler = new GetDeploymentCadenceReport.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetDeploymentCadenceReport.Query(30), CancellationToken.None);

        result.Value.Services[0].Cadence.Should().Be(DeploymentCadence.Medium);
    }

    [Fact]
    public async Task GetDeploymentCadenceReport_Distribution_Is_Correct()
    {
        var highPerformerReleases = Enumerable.Range(0, 30)
            .Select(i => MakeRelease("fast-svc", FixedNow.AddDays(-i)));
        var lowPerformerReleases = Enumerable.Range(0, 2)
            .Select(i => MakeRelease("slow-svc", FixedNow.AddDays(-i * 10)));
        var all = highPerformerReleases.Concat(lowPerformerReleases).ToList();

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)all);

        var handler = new GetDeploymentCadenceReport.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetDeploymentCadenceReport.Query(30), CancellationToken.None);

        result.Value.Distribution.HighPerformer.Should().Be(1);
        result.Value.Distribution.LowPerformer.Should().Be(1);
        result.Value.TotalServices.Should().Be(2);
    }

    [Fact]
    public void GetDeploymentCadenceReport_Validator_Rejects_Invalid_Days()
    {
        var validator = new GetDeploymentCadenceReport.Validator();
        var low = validator.Validate(new GetDeploymentCadenceReport.Query(6));
        var high = validator.Validate(new GetDeploymentCadenceReport.Query(91));
        low.IsValid.Should().BeFalse();
        high.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetDeploymentCadenceReport_Validator_Accepts_Valid_Query()
    {
        var validator = new GetDeploymentCadenceReport.Validator();
        var result = validator.Validate(new GetDeploymentCadenceReport.Query(30));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetDeploymentCadenceReport_Services_Ordered_By_DeploysPerDay_Desc()
    {
        var many = Enumerable.Range(0, 20).Select(i => MakeRelease("fast-svc", FixedNow.AddDays(-i))).ToList();
        var few = Enumerable.Range(0, 2).Select(i => MakeRelease("slow-svc", FixedNow.AddDays(-i * 10))).ToList();
        var all = many.Concat(few).ToList();

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)all);

        var handler = new GetDeploymentCadenceReport.Handler(releaseRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetDeploymentCadenceReport.Query(30), CancellationToken.None);

        result.Value.Services.First().ServiceName.Should().Be("fast-svc");
    }
}
