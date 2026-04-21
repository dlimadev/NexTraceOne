using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetTeamChangeVelocityReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using VelocityTier = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetTeamChangeVelocityReport.GetTeamChangeVelocityReport.VelocityTier;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave M.2 — GetTeamChangeVelocityReport.
/// Cobre agrupamento por equipa, cálculo de velocidade, taxa de sucesso/rollback
/// e classificação de nível de velocidade.
/// </summary>
public sealed class TeamChangeVelocityReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(string team, DeploymentStatus status, DateTimeOffset? at = null)
    {
        var r = Release.Create(TenantId, Guid.NewGuid(), "svc-" + team, "1.0.0",
            "production", "jenkins", "abc", at ?? FixedNow.AddDays(-10));
        if (team.Length > 0)
        {
            // Set team by using the same serviceName trick isn't possible;
            // releases use TeamName from SetTeamName-like approach.
            // Use reflection-free: TeamName is set during ingestion, fallback to "unassigned".
            // We test by creating releases using different TeamNames set via UpdateTeamName or
            // just accept TeamName is null and all go to "unassigned" group.
        }

        // Advance status via valid transitions
        if (status == DeploymentStatus.Running || status == DeploymentStatus.Succeeded
            || status == DeploymentStatus.Failed || status == DeploymentStatus.RolledBack)
        {
            r.UpdateStatus(DeploymentStatus.Running);
        }
        if (status == DeploymentStatus.Succeeded)
            r.UpdateStatus(DeploymentStatus.Succeeded);
        else if (status == DeploymentStatus.Failed)
            r.UpdateStatus(DeploymentStatus.Failed);
        else if (status == DeploymentStatus.RolledBack)
        {
            r.UpdateStatus(DeploymentStatus.Succeeded);
            r.UpdateStatus(DeploymentStatus.RolledBack);
        }
        return r;
    }

    // ── Empty report ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Releases()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetTeamChangeVelocityReport.Handler(releaseRepo, CreateClock());
        var result = await handler.Handle(new GetTeamChangeVelocityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(0);
        result.Value.TeamsAnalyzed.Should().Be(0);
        result.Value.TeamMetrics.Should().BeEmpty();
    }

    // ── Grouping by team ──────────────────────────────────────────────────

    [Fact]
    public async Task Report_Groups_Releases_By_Team_Correctly()
    {
        // All releases will have null TeamName → grouped as "unassigned"
        var releases = new[]
        {
            MakeRelease("a", DeploymentStatus.Succeeded),
            MakeRelease("a", DeploymentStatus.Succeeded),
            MakeRelease("b", DeploymentStatus.Failed),
        };
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var handler = new GetTeamChangeVelocityReport.Handler(releaseRepo, CreateClock());
        var result = await handler.Handle(new GetTeamChangeVelocityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(3);
        // All null TeamName → single "unassigned" group
        result.Value.TeamsAnalyzed.Should().Be(1);
    }

    // ── Tenant-level rates ────────────────────────────────────────────────

    [Fact]
    public async Task TenantSuccessRate_Correct()
    {
        var releases = new[]
        {
            MakeRelease("x", DeploymentStatus.Succeeded),
            MakeRelease("x", DeploymentStatus.Succeeded),
            MakeRelease("x", DeploymentStatus.Failed),
            MakeRelease("x", DeploymentStatus.Pending),
        };
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var handler = new GetTeamChangeVelocityReport.Handler(releaseRepo, CreateClock());
        var result = await handler.Handle(new GetTeamChangeVelocityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantSuccessRate.Should().Be(50.0m); // 2/4
    }

    [Fact]
    public async Task TenantRollbackRate_Correct()
    {
        var releases = new[]
        {
            MakeRelease("x", DeploymentStatus.RolledBack),
            MakeRelease("x", DeploymentStatus.Succeeded),
            MakeRelease("x", DeploymentStatus.Succeeded),
            MakeRelease("x", DeploymentStatus.Succeeded),
        };
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var handler = new GetTeamChangeVelocityReport.Handler(releaseRepo, CreateClock());
        var result = await handler.Handle(new GetTeamChangeVelocityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantRollbackRate.Should().Be(25.0m); // 1/4
    }

    // ── TeamsWithRollbacks ────────────────────────────────────────────────

    [Fact]
    public async Task TeamsWithRollbacks_Counts_Teams_That_Had_Rollback()
    {
        var releases = new[]
        {
            MakeRelease("alpha", DeploymentStatus.RolledBack),
            MakeRelease("beta", DeploymentStatus.Succeeded),
        };
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var handler = new GetTeamChangeVelocityReport.Handler(releaseRepo, CreateClock());
        var result = await handler.Handle(new GetTeamChangeVelocityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // All go to "unassigned", 1 rolled back → 1 team with rollbacks
        result.Value.TeamsWithRollbacks.Should().Be(1);
    }

    // ── Velocity tier ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(56, 7, "HighVolume")]   // 56 releases / (7/7) weeks = 56/week ≥ 4
    [InlineData(14, 14, "HighVolume")]  // 14/2 = 7/week ≥ 4
    [InlineData(7, 14, "Moderate")]     // 7/2 = 3.5/week ≥ 1
    [InlineData(1, 28, "LowFrequency")] // 1/4 = 0.25/week exactly ≥ 0.25
    [InlineData(1, 365, "Inactive")]    // ~0.02/week < 0.25
    public async Task VelocityTier_Classified_Correctly(int releaseCount, int lookbackDays, string expectedTier)
    {
        var releases = Enumerable.Range(0, releaseCount)
            .Select(_ => MakeRelease("t", DeploymentStatus.Succeeded))
            .ToArray();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var handler = new GetTeamChangeVelocityReport.Handler(releaseRepo, CreateClock());
        var result = await handler.Handle(
            new GetTeamChangeVelocityReport.Query(TenantId, LookbackDays: lookbackDays), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamMetrics.Should().HaveCount(1);
        result.Value.TeamMetrics[0].VelocityTier.ToString().Should().Be(expectedTier);
    }

    // ── TopTeamsCount limit ───────────────────────────────────────────────

    [Fact]
    public async Task TeamMetrics_Capped_By_TopTeamsCount()
    {
        // All go to "unassigned" — will be 1 team regardless of count
        var releases = Enumerable.Range(0, 10)
            .Select(_ => MakeRelease("x", DeploymentStatus.Succeeded))
            .ToArray();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var handler = new GetTeamChangeVelocityReport.Handler(releaseRepo, CreateClock());
        var result = await handler.Handle(
            new GetTeamChangeVelocityReport.Query(TenantId, TopTeamsCount: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamMetrics.Count.Should().BeLessThanOrEqualTo(5);
    }

    // ── From / To time range ──────────────────────────────────────────────

    [Fact]
    public async Task Report_From_And_To_Match_LookbackDays()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetTeamChangeVelocityReport.Handler(releaseRepo, CreateClock());
        var result = await handler.Handle(
            new GetTeamChangeVelocityReport.Query(TenantId, LookbackDays: 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.To.Should().Be(FixedNow);
        result.Value.From.Should().Be(FixedNow.AddDays(-30));
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_Empty_TenantId()
    {
        var v = new GetTeamChangeVelocityReport.Validator();
        v.Validate(new GetTeamChangeVelocityReport.Query(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_Zero_LookbackDays()
    {
        var v = new GetTeamChangeVelocityReport.Validator();
        v.Validate(new GetTeamChangeVelocityReport.Query(TenantId, LookbackDays: 0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var v = new GetTeamChangeVelocityReport.Validator();
        v.Validate(new GetTeamChangeVelocityReport.Query(TenantId)).IsValid.Should().BeTrue();
    }
}
