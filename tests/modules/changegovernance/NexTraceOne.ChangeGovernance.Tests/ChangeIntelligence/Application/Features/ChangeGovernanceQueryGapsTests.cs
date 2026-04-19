using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseCalendar;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseHistory;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRiskScoreTrend;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRollbackAssessment;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListFreezeWindows;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes de unidade para features sem cobertura prévia:
/// GetBlastRadiusReport, GetRollbackAssessment, GetReleaseHistory,
/// GetReleaseCalendar, GetRiskScoreTrend, ListFreezeWindows.
/// </summary>
public sealed class ChangeGovernanceQueryGapsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    private static Release MakeRelease(string service = "order-service", string version = "1.0.0", string env = "production")
        => Release.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            service, version, env,
            "GitLab CI", "abc123def456",
            FixedNow.AddHours(-2));

    private static BlastRadiusReport MakeBlastRadiusReport(ReleaseId releaseId)
        => BlastRadiusReport.Calculate(
            releaseId, Guid.NewGuid(),
            ["consumer-a", "consumer-b"],
            ["transitive-c"],
            FixedNow);

    private static RollbackAssessment MakeRollbackAssessment(ReleaseId releaseId, bool viable = true)
        => RollbackAssessment.Create(
            releaseId,
            isViable: viable,
            readinessScore: viable ? 0.9m : 0.1m,
            previousVersion: "0.9.0",
            hasReversibleMigrations: true,
            consumersAlreadyMigrated: 2,
            totalConsumersImpacted: 5,
            inviabilityReason: viable ? null : "Migration is irreversible",
            recommendation: viable ? "Safe to rollback" : "Do not rollback — migration has no Down()",
            assessedAt: FixedNow);

    private static FreezeWindow MakeFreezeWindow(bool active = true)
        => FreezeWindow.Create(
            "Holiday Freeze", "No deploys during holiday",
            FreezeScope.Global, null,
            FixedNow.AddDays(-1), FixedNow.AddDays(5),
            "admin@nextraceone.local", FixedNow.AddDays(-2));

    // ═══════════════════════════════════════════════════════════════════
    // GetBlastRadiusReport
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBlastRadiusReport_Should_ReturnReport_When_Exists()
    {
        var release = MakeRelease();
        var report = MakeBlastRadiusReport(release.Id);

        var repo = Substitute.For<IBlastRadiusRepository>();
        repo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(report);

        var handler = new GetBlastRadiusReport.Handler(repo);
        var result = await handler.Handle(new GetBlastRadiusReport.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.TotalAffectedConsumers.Should().Be(3);
        result.Value.DirectConsumers.Should().HaveCount(2);
        result.Value.TransitiveConsumers.Should().HaveCount(1);
        result.Value.CalculatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetBlastRadiusReport_Should_ReturnError_When_NotFound()
    {
        var repo = Substitute.For<IBlastRadiusRepository>();
        repo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((BlastRadiusReport?)null);

        var handler = new GetBlastRadiusReport.Handler(repo);
        var result = await handler.Handle(new GetBlastRadiusReport.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ChangeIntelligence.BlastRadiusReport.NotFound");
    }

    [Fact]
    public void GetBlastRadiusReport_Validator_Should_Reject_EmptyId()
    {
        var validator = new GetBlastRadiusReport.Validator();
        var result = validator.Validate(new GetBlastRadiusReport.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetRollbackAssessment
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRollbackAssessment_Should_ReturnAssessment_When_ReleaseAndAssessmentExist()
    {
        var release = MakeRelease();
        var assessment = MakeRollbackAssessment(release.Id);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);

        var assessmentRepo = Substitute.For<IRollbackAssessmentRepository>();
        assessmentRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(assessment);

        var handler = new GetRollbackAssessment.Handler(releaseRepo, assessmentRepo);
        var result = await handler.Handle(new GetRollbackAssessment.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.ServiceName.Should().Be("order-service");
        result.Value.IsViable.Should().BeTrue();
        result.Value.ReadinessScore.Should().Be(0.9m);
        result.Value.PreviousVersion.Should().Be("0.9.0");
    }

    [Fact]
    public async Task GetRollbackAssessment_Should_ReturnError_When_ReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((Release?)null);
        var assessmentRepo = Substitute.For<IRollbackAssessmentRepository>();

        var handler = new GetRollbackAssessment.Handler(releaseRepo, assessmentRepo);
        var result = await handler.Handle(new GetRollbackAssessment.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ChangeIntelligence.Release.NotFound");
    }

    [Fact]
    public async Task GetRollbackAssessment_Should_ReturnError_When_AssessmentNotFound()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);

        var assessmentRepo = Substitute.For<IRollbackAssessmentRepository>();
        assessmentRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((RollbackAssessment?)null);

        var handler = new GetRollbackAssessment.Handler(releaseRepo, assessmentRepo);
        var result = await handler.Handle(new GetRollbackAssessment.Query(release.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ChangeIntelligence.RollbackAssessment.NotFound");
    }

    [Fact]
    public void GetRollbackAssessment_Validator_Should_Reject_EmptyId()
    {
        var validator = new GetRollbackAssessment.Validator();
        var result = validator.Validate(new GetRollbackAssessment.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetReleaseHistory
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetReleaseHistory_Should_ReturnPaginatedReleases()
    {
        var releases = new List<Release> { MakeRelease("api-a", "2.0.0"), MakeRelease("api-a", "1.9.0") };
        var assetId = Guid.NewGuid();

        var repo = Substitute.For<IReleaseRepository>();
        repo.ListByApiAssetAsync(assetId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)releases);
        repo.CountByApiAssetAsync(assetId, Arg.Any<CancellationToken>()).Returns(2);

        var handler = new GetReleaseHistory.Handler(repo);
        var result = await handler.Handle(new GetReleaseHistory.Query(assetId, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Releases.Select(r => r.Version).Should().Contain("2.0.0");
    }

    [Fact]
    public async Task GetReleaseHistory_Should_ReturnEmpty_When_NoReleases()
    {
        var assetId = Guid.NewGuid();
        var repo = Substitute.For<IReleaseRepository>();
        repo.ListByApiAssetAsync(assetId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)new List<Release>());
        repo.CountByApiAssetAsync(assetId, Arg.Any<CancellationToken>()).Returns(0);

        var handler = new GetReleaseHistory.Handler(repo);
        var result = await handler.Handle(new GetReleaseHistory.Query(assetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public void GetReleaseHistory_Validator_Should_Reject_InvalidPageSize()
    {
        var validator = new GetReleaseHistory.Validator();
        var result = validator.Validate(new GetReleaseHistory.Query(Guid.NewGuid(), 1, 200));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetReleaseHistory_Validator_Should_Reject_EmptyAssetId()
    {
        var validator = new GetReleaseHistory.Validator();
        var result = validator.Validate(new GetReleaseHistory.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetReleaseCalendar
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetReleaseCalendar_Should_AggregateReleasesAndFreezeWindows()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.NewGuid());

        var releases = new List<Release> { MakeRelease(), MakeRelease("billing-service", "1.0.1") };
        var freezes = new List<FreezeWindow> { MakeFreezeWindow() };

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)releases);

        var freezeRepo = Substitute.For<IFreezeWindowRepository>();
        freezeRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<FreezeWindow>)freezes);

        var handler = new GetReleaseCalendar.Handler(releaseRepo, freezeRepo, tenant);
        var result = await handler.Handle(
            new GetReleaseCalendar.Query(FixedNow.AddDays(-7), FixedNow.AddDays(7), "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().HaveCount(2);
        result.Value.FreezeWindows.Should().HaveCount(1);
        result.Value.DailySummary.Should().NotBeNull();
        result.Value.DailySummary.Should().AllSatisfy(ds => ds.TotalReleases.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetReleaseCalendar_Should_ReturnEmpty_When_NoData()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.NewGuid());

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)new List<Release>());

        var freezeRepo = Substitute.For<IFreezeWindowRepository>();
        freezeRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<FreezeWindow>)new List<FreezeWindow>());

        var handler = new GetReleaseCalendar.Handler(releaseRepo, freezeRepo, tenant);
        var result = await handler.Handle(
            new GetReleaseCalendar.Query(FixedNow.AddDays(-7), FixedNow.AddDays(7), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().BeEmpty();
        result.Value.FreezeWindows.Should().BeEmpty();
        result.Value.DailySummary.Should().BeEmpty();
    }

    [Fact]
    public void GetReleaseCalendar_Validator_Should_Reject_WhenToBeforeFrom()
    {
        var validator = new GetReleaseCalendar.Validator();
        var result = validator.Validate(new GetReleaseCalendar.Query(FixedNow.AddDays(1), FixedNow, null));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetRiskScoreTrend
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRiskScoreTrend_Should_ReturnDataPoints_ForService()
    {
        var releases = new List<Release> { MakeRelease(), MakeRelease("order-service", "0.9.0") };
        var score = ChangeIntelligenceScore.Compute(releases[0].Id, 0.6m, 0.3m, 0.2m, FixedNow);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListByServiceNameAsync("order-service", 1, 30, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)releases);

        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        scoreRepo.GetByReleaseIdAsync(releases[0].Id, Arg.Any<CancellationToken>()).Returns(score);
        scoreRepo.GetByReleaseIdAsync(releases[1].Id, Arg.Any<CancellationToken>()).Returns((ChangeIntelligenceScore?)null);

        var handler = new GetRiskScoreTrend.Handler(releaseRepo, scoreRepo, CreateClock());
        var result = await handler.Handle(
            new GetRiskScoreTrend.Query("order-service", "production", Limit: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("order-service");
        result.Value.DataPoints.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetRiskScoreTrend_Should_ReturnEmptyTrend_When_NoReleases()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListByServiceNameAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)new List<Release>());

        var scoreRepo = Substitute.For<IChangeScoreRepository>();

        var handler = new GetRiskScoreTrend.Handler(releaseRepo, scoreRepo, CreateClock());
        var result = await handler.Handle(
            new GetRiskScoreTrend.Query("unknown-service", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DataPoints.Should().BeEmpty();
    }

    [Fact]
    public void GetRiskScoreTrend_Validator_Should_Reject_EmptyServiceName()
    {
        var validator = new GetRiskScoreTrend.Validator();
        var result = validator.Validate(new GetRiskScoreTrend.Query("", null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetRiskScoreTrend_Validator_Should_Reject_LimitOver100()
    {
        var validator = new GetRiskScoreTrend.Validator();
        var result = validator.Validate(new GetRiskScoreTrend.Query("svc", null, 101));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ListFreezeWindows
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListFreezeWindows_Should_ReturnPaginatedFreezeWindows()
    {
        var windows = new List<FreezeWindow> { MakeFreezeWindow(), MakeFreezeWindow() };

        var repo = Substitute.For<IFreezeWindowRepository>();
        repo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<FreezeWindow>)windows);

        var handler = new ListFreezeWindows.Handler(repo);
        var result = await handler.Handle(
            new ListFreezeWindows.Query(FixedNow.AddDays(-7), FixedNow.AddDays(7), null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListFreezeWindows_Should_ReturnEmpty_When_None()
    {
        var repo = Substitute.For<IFreezeWindowRepository>();
        repo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<FreezeWindow>)new List<FreezeWindow>());

        var handler = new ListFreezeWindows.Handler(repo);
        var result = await handler.Handle(
            new ListFreezeWindows.Query(FixedNow.AddDays(-7), FixedNow.AddDays(7), null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void ListFreezeWindows_Validator_Should_Accept_ValidQuery()
    {
        var validator = new ListFreezeWindows.Validator();
        var result = validator.Validate(
            new ListFreezeWindows.Query(FixedNow.AddDays(-7), FixedNow.AddDays(7), null, null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListFreezeWindows_Validator_Should_Reject_WhenToBeforeFrom()
    {
        var validator = new ListFreezeWindows.Validator();
        var result = validator.Validate(
            new ListFreezeWindows.Query(FixedNow.AddDays(1), FixedNow.AddDays(-1), null, null));
        result.IsValid.Should().BeFalse();
    }
}
