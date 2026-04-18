using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

using GetChangesSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangesSummary.GetChangesSummary;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para o handler GetChangesSummary.
/// Verifica contadores agregados de mudanças para dashboards de Change Confidence.
/// </summary>
public sealed class GetChangesSummaryTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 18, 21, 0, 0, TimeSpan.Zero);

    private readonly IReleaseRepository _repo = Substitute.For<IReleaseRepository>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();

    private GetChangesSummaryFeature.Handler CreateHandler() =>
        new(_repo, _tenant);

    // ── Aggregate counters ─────────────────────────────────────────────────

    [Fact]
    public async Task GetChangesSummary_WithValidData_ShouldReturnAggregatedCounters()
    {
        _tenant.Id.Returns(Guid.NewGuid());
        _repo.GetSummaryCountsAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns((50, 38, 7, 3, 5));

        var sut = CreateHandler();
        var result = await sut.Handle(
            new GetChangesSummaryFeature.Query(null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalChanges.Should().Be(50);
        result.Value.ValidatedChanges.Should().Be(38);
        result.Value.ChangesNeedingAttention.Should().Be(7);
        result.Value.SuspectedRegressions.Should().Be(3);
        result.Value.ChangesCorrelatedWithIncidents.Should().Be(5);
    }

    [Fact]
    public async Task GetChangesSummary_WithNoChanges_ShouldReturnAllZeros()
    {
        _tenant.Id.Returns(Guid.NewGuid());
        _repo.GetSummaryCountsAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns((0, 0, 0, 0, 0));

        var sut = CreateHandler();
        var result = await sut.Handle(
            new GetChangesSummaryFeature.Query("team-alpha", "Production", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalChanges.Should().Be(0);
        result.Value.ValidatedChanges.Should().Be(0);
    }

    [Fact]
    public async Task GetChangesSummary_PassesFiltersToRepository()
    {
        var tenantId = Guid.NewGuid();
        _tenant.Id.Returns(tenantId);
        _repo.GetSummaryCountsAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns((10, 8, 1, 0, 1));

        var from = FixedNow.AddDays(-30);
        var to = FixedNow;

        var sut = CreateHandler();
        await sut.Handle(
            new GetChangesSummaryFeature.Query("team-platform", "Staging", from, to),
            CancellationToken.None);

        await _repo.Received(1).GetSummaryCountsAsync(
            tenantId, "team-platform", "Staging", from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetChangesSummary_WithTeamFilter_ShouldPassTeamNameToRepository()
    {
        _tenant.Id.Returns(Guid.NewGuid());
        _repo.GetSummaryCountsAsync(
            Arg.Any<Guid>(), Arg.Is<string?>("team-payments"), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns((20, 15, 3, 1, 2));

        var sut = CreateHandler();
        var result = await sut.Handle(
            new GetChangesSummaryFeature.Query("team-payments", null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalChanges.Should().Be(20);
    }
}
