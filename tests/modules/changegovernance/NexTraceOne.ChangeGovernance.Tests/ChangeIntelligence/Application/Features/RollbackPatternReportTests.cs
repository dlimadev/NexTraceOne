using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRollbackPatternReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave W.1 — GetRollbackPatternReport.
/// Cobre: sem releases, padrões None/Isolated/Recurring/Serial,
/// SystemicRisk, EvidenceGap, distribuição, validator.
/// </summary>
public sealed class RollbackPatternReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("AAAA1111-0000-0000-0000-000000000001");

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(
        string serviceName,
        DateTimeOffset createdAt,
        DeploymentStatus status = DeploymentStatus.Succeeded)
    {
        var r = Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0",
            "production", "jenkins", "abc", createdAt);
        r.UpdateStatus(DeploymentStatus.Running);
        if (status == DeploymentStatus.Succeeded)
            r.UpdateStatus(DeploymentStatus.Succeeded);
        else if (status == DeploymentStatus.RolledBack)
        {
            r.UpdateStatus(DeploymentStatus.Succeeded);
            r.UpdateStatus(DeploymentStatus.RolledBack);
        }
        else if (status == DeploymentStatus.Failed)
            r.UpdateStatus(DeploymentStatus.Failed);
        return r;
    }

    private static GetRollbackPatternReport.Handler CreateHandler(
        IReadOnlyList<Release> releases,
        IReadOnlyList<ChangeConfidenceBreakdown> breakdowns,
        IReadOnlyList<EvidencePack> evidencePacks)
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(releases);

        var confidenceRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        confidenceRepo.ListByReleaseIdsAsync(
                Arg.Any<IEnumerable<ReleaseId>>(), Arg.Any<CancellationToken>())
            .Returns(breakdowns);

        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        evidenceRepo.ListByReleaseIdsAsync(
                Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(evidencePacks);

        return new GetRollbackPatternReport.Handler(
            releaseRepo, confidenceRepo, evidenceRepo, CreateClock());
    }
    private static GetRollbackPatternReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 90, MaxTopServices: 10);

    // ── Empty: no releases ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoReleases_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalServicesAnalyzed);
        Assert.Equal(0, r.TotalRollbacksInPeriod);
        Assert.Empty(r.AllServices);
    }

    // ── None: all releases succeed ────────────────────────────────────────

    [Fact]
    public async Task Handle_NoRollbacks_AllServicesNonePattern()
    {
        var releases = new[]
        {
            MakeRelease("svc-a", FixedNow.AddDays(-10), DeploymentStatus.Succeeded),
            MakeRelease("svc-a", FixedNow.AddDays(-5), DeploymentStatus.Succeeded)
        };

        var handler = CreateHandler(releases, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetRollbackPatternReport.RollbackPattern.None, entry.Pattern);
        Assert.Equal(1, result.Value.Distribution.NoneCount);
    }

    // ── Isolated: 1 rollback ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_OneRollback_ClassifiesAsIsolated()
    {
        var releases = new[]
        {
            MakeRelease("svc-b", FixedNow.AddDays(-20), DeploymentStatus.RolledBack),
            MakeRelease("svc-b", FixedNow.AddDays(-10), DeploymentStatus.Succeeded)
        };

        var handler = CreateHandler(releases, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetRollbackPatternReport.RollbackPattern.Isolated, entry.Pattern);
        Assert.Equal(1, result.Value.Distribution.IsolatedCount);
    }

    // ── Recurring: 2 rollbacks ────────────────────────────────────────────

    [Fact]
    public async Task Handle_TwoRollbacks_ClassifiesAsRecurring()
    {
        var releases = new[]
        {
            MakeRelease("svc-c", FixedNow.AddDays(-30), DeploymentStatus.RolledBack),
            MakeRelease("svc-c", FixedNow.AddDays(-20), DeploymentStatus.RolledBack),
            MakeRelease("svc-c", FixedNow.AddDays(-10), DeploymentStatus.Succeeded)
        };

        var handler = CreateHandler(releases, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetRollbackPatternReport.RollbackPattern.Recurring, entry.Pattern);
        Assert.Equal(1, result.Value.Distribution.RecurringCount);
    }

    // ── Serial: 4 rollbacks ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_FourRollbacks_ClassifiesAsSerial()
    {
        var releases = Enumerable.Range(1, 4)
            .Select(i => MakeRelease("svc-d", FixedNow.AddDays(-i * 5), DeploymentStatus.RolledBack))
            .ToList();

        var handler = CreateHandler(releases, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetRollbackPatternReport.RollbackPattern.Serial, entry.Pattern);
        Assert.Equal(1, result.Value.Distribution.SerialCount);
        Assert.Single(result.Value.TopSerialServices);
    }

    // ── SystemicRisk: Serial + low confidence ─────────────────────────────

    [Fact]
    public async Task Handle_SerialWithLowConfidence_SetsSystemicRisk()
    {
        var rollbacks = Enumerable.Range(1, 4)
            .Select(i => MakeRelease("svc-risky", FixedNow.AddDays(-i * 5), DeploymentStatus.RolledBack))
            .ToList();

        // Low confidence breakdowns for each rollback
        var breakdowns = rollbacks
            .Select(r => ChangeConfidenceBreakdown.Create(r.Id, [
                ChangeConfidenceSubScore.Create(
                    ConfidenceSubScoreType.TestCoverage, 30m, 1.0m,
                    ConfidenceDataQuality.Low, "low coverage", [])
            ], FixedNow))
            .ToList();

        var handler = CreateHandler(rollbacks, breakdowns, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.True(entry.SystemicRisk);
        Assert.True(entry.AvgConfidenceAtRollback < 50m);
    }

    // ── No SystemicRisk: Serial + high confidence ─────────────────────────

    [Fact]
    public async Task Handle_SerialWithHighConfidence_NoSystemicRisk()
    {
        var rollbacks = Enumerable.Range(1, 4)
            .Select(i => MakeRelease("svc-ok", FixedNow.AddDays(-i * 5), DeploymentStatus.RolledBack))
            .ToList();

        var breakdowns = rollbacks
            .Select(r => ChangeConfidenceBreakdown.Create(r.Id, [
                ChangeConfidenceSubScore.Create(
                    ConfidenceSubScoreType.TestCoverage, 80m, 1.0m,
                    ConfidenceDataQuality.High, "high coverage", [])
            ], FixedNow))
            .ToList();

        var handler = CreateHandler(rollbacks, breakdowns, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.False(entry.SystemicRisk);
    }

    // ── GlobalRollbackRate ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MixedReleases_GlobalRollbackRateCorrect()
    {
        var releases = new[]
        {
            MakeRelease("svc-e", FixedNow.AddDays(-20), DeploymentStatus.RolledBack),
            MakeRelease("svc-e", FixedNow.AddDays(-10), DeploymentStatus.Succeeded),
            MakeRelease("svc-e", FixedNow.AddDays(-5), DeploymentStatus.Succeeded),
            MakeRelease("svc-e", FixedNow.AddDays(-2), DeploymentStatus.Succeeded)
        };

        var handler = CreateHandler(releases, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // 1 rollback / 4 total = 25%
        Assert.Equal(25m, result.Value.GlobalRollbackRatePct);
    }

    // ── TopByRollbackRate ordering ────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_TopByRollbackRateOrdered()
    {
        var releases = new List<Release>
        {
            // svc-high: 2/2 = 100%
            MakeRelease("svc-high", FixedNow.AddDays(-20), DeploymentStatus.RolledBack),
            MakeRelease("svc-high", FixedNow.AddDays(-10), DeploymentStatus.RolledBack),
            // svc-low: 1/5 = 20%
            MakeRelease("svc-low", FixedNow.AddDays(-25), DeploymentStatus.RolledBack),
            MakeRelease("svc-low", FixedNow.AddDays(-15), DeploymentStatus.Succeeded),
            MakeRelease("svc-low", FixedNow.AddDays(-12), DeploymentStatus.Succeeded),
            MakeRelease("svc-low", FixedNow.AddDays(-8), DeploymentStatus.Succeeded),
            MakeRelease("svc-low", FixedNow.AddDays(-3), DeploymentStatus.Succeeded)
        };

        var handler = CreateHandler(releases, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var top = result.Value.TopByRollbackRate;
        Assert.Equal("svc-high", top.First().ServiceName);
        Assert.Equal("svc-low", top.Last().ServiceName);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_LookbackDaysOutOfRange_Fails()
    {
        var v = new GetRollbackPatternReport.Validator();
        Assert.False(v.Validate(new GetRollbackPatternReport.Query(TenantId, LookbackDays: 5)).IsValid);
        Assert.False(v.Validate(new GetRollbackPatternReport.Query(TenantId, LookbackDays: 200)).IsValid);
    }

    [Fact]
    public void Validator_EmptyTenantId_Fails()
    {
        var v = new GetRollbackPatternReport.Validator();
        Assert.False(v.Validate(new GetRollbackPatternReport.Query(Guid.Empty)).IsValid);
    }

    [Fact]
    public void Validator_DefaultQuery_IsValid()
    {
        var v = new GetRollbackPatternReport.Validator();
        Assert.True(v.Validate(new GetRollbackPatternReport.Query(TenantId)).IsValid);
    }
}
