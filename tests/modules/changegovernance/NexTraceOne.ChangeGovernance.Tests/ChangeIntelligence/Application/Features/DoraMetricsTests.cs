using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

using GetDoraMetricsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDoraMetrics.GetDoraMetrics;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para o handler GetDoraMetrics.
/// Verifica cálculo das 4 métricas DORA, classificações e comportamento com DORA desabilitado.
/// </summary>
public sealed class DoraMetricsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 18, 21, 0, 0, TimeSpan.Zero);

    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IIncidentModule _incidentModule = Substitute.For<IIncidentModule>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IEnvironmentBehaviorService _envBehavior = Substitute.For<IEnvironmentBehaviorService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public DoraMetricsTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private static Release CreateRelease(
        string service = "svc-payments",
        string env = "Production",
        DeploymentStatus status = DeploymentStatus.Succeeded)
    {
        var r = Release.Create(
            Guid.NewGuid(), Guid.Empty, service, "1.0.0", env,
            "https://ci/pipeline", "abc123", FixedNow.AddDays(-2));
        if (status == DeploymentStatus.Succeeded)
        {
            r.UpdateStatus(DeploymentStatus.Running);
            r.UpdateStatus(DeploymentStatus.Succeeded);
        }
        else if (status == DeploymentStatus.Failed)
        {
            r.UpdateStatus(DeploymentStatus.Running);
            r.UpdateStatus(DeploymentStatus.Failed);
        }
        return r;
    }

    private void SetupCountFiltered(DeploymentStatus status, int count)
    {
        _releaseRepo.CountFilteredAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<ChangeType?>(), Arg.Any<ConfidenceStatus?>(),
            Arg.Is<DeploymentStatus?>(s => s == status),
            Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>()).Returns(count);
    }

    private GetDoraMetricsFeature.Handler CreateHandler() =>
        new(_releaseRepo, _incidentModule, _currentTenant, _envBehavior, _clock);

    // ── DORA disabled gate ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDoraMetrics_WhenDoraDisabled_ShouldReturnEmptyResponse()
    {
        _envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _currentTenant.Id.Returns(Guid.NewGuid());

        var sut = CreateHandler();
        var result = await sut.Handle(new GetDoraMetricsFeature.Query(Days: 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeploymentFrequency.TotalDeploys.Should().Be(0);
        result.Value.OverallClassification.Should().Be(GetDoraMetricsFeature.DoraClassification.Low);
    }

    [Fact]
    public async Task GetDoraMetrics_WhenDoraDisabled_GeneratedAt_ShouldUseClockUtcNow()
    {
        _envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _currentTenant.Id.Returns(Guid.NewGuid());

        var sut = CreateHandler();
        var result = await sut.Handle(new GetDoraMetricsFeature.Query(Days: 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Deployment Frequency ───────────────────────────────────────────────

    [Fact]
    public async Task GetDoraMetrics_WithEliteDeployFrequency_ShouldClassifyElite()
    {
        _envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _currentTenant.Id.Returns(Guid.NewGuid());

        // 31 deploys in 30 days → >1/day → Elite
        SetupCountFiltered(DeploymentStatus.Succeeded, 31);
        SetupCountFiltered(DeploymentStatus.Failed, 0);
        SetupCountFiltered(DeploymentStatus.RolledBack, 0);

        var releases = Enumerable.Range(0, 31).Select(_ => CreateRelease()).ToList<Release>();
        _releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(),
            Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(releases);

        _incidentModule.GetAverageResolutionHoursAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(0.5m);

        var sut = CreateHandler();
        var result = await sut.Handle(new GetDoraMetricsFeature.Query(Days: 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeploymentFrequency.Classification.Should().Be(GetDoraMetricsFeature.DoraClassification.Elite);
        result.Value.DeploymentFrequency.TotalDeploys.Should().Be(31);
    }

    [Fact]
    public async Task GetDoraMetrics_Enabled_TimeWindow_ShouldDeriveFromClockUtcNow()
    {
        // Arrange — verify that the query window ends at FixedNow
        _envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _currentTenant.Id.Returns(Guid.NewGuid());

        SetupCountFiltered(DeploymentStatus.Succeeded, 0);
        SetupCountFiltered(DeploymentStatus.Failed, 0);
        SetupCountFiltered(DeploymentStatus.RolledBack, 0);
        _releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(),
            Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<Release>());
        _incidentModule.GetAverageResolutionHoursAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        var sut = CreateHandler();
        var result = await sut.Handle(new GetDoraMetricsFeature.Query(Days: 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedAt.Should().Be(FixedNow);

        // ListInRangeAsync must have been called with [FixedNow-30d, FixedNow]
        await _releaseRepo.Received(1).ListInRangeAsync(
            Arg.Is<DateTimeOffset>(d => d == FixedNow.AddDays(-30)),
            Arg.Is<DateTimeOffset>(d => d == FixedNow),
            Arg.Any<string?>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    // ── Change Failure Rate ────────────────────────────────────────────────

    [Fact]
    public async Task GetDoraMetrics_WithHighFailureRate_ShouldClassifyFailureRateLow()
    {
        _envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _currentTenant.Id.Returns(Guid.NewGuid());

        // 5 succeeded, 20 failed → 80% failure → Low
        SetupCountFiltered(DeploymentStatus.Succeeded, 5);
        SetupCountFiltered(DeploymentStatus.Failed, 20);
        SetupCountFiltered(DeploymentStatus.RolledBack, 0);

        _releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(),
            Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<Release>());

        _incidentModule.GetAverageResolutionHoursAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        var sut = CreateHandler();
        var result = await sut.Handle(new GetDoraMetricsFeature.Query(Days: 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeFailureRate.FailurePercentage.Should().BeGreaterThan(15m);
        result.Value.ChangeFailureRate.Classification.Should().Be(GetDoraMetricsFeature.DoraClassification.Low);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    public void GetDoraMetrics_Validator_WithInvalidDays_ShouldFail(int days)
    {
        var validator = new GetDoraMetricsFeature.Validator();
        var result = validator.Validate(new GetDoraMetricsFeature.Query(Days: days));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public void GetDoraMetrics_Validator_WithValidDays_ShouldPass(int days)
    {
        var validator = new GetDoraMetricsFeature.Validator();
        var result = validator.Validate(new GetDoraMetricsFeature.Query(Days: days));
        result.IsValid.Should().BeTrue();
    }
}
