using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.ComputeDeveloperExperienceScore;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperExperienceScore;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.ListDeveloperExperienceScores;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.RecordProductivitySnapshot;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;
using Xunit;

namespace NexTraceOne.Catalog.Tests.DeveloperExperience;

/// <summary>
/// Testes unitários para o subdomínio Developer Experience (Phase 5.2).
/// Cobrem: DxScore, ProductivitySnapshot, ComputeDeveloperExperienceScore,
/// GetDeveloperExperienceScore, ListDeveloperExperienceScores, RecordProductivitySnapshot.
/// </summary>
public sealed class DeveloperExperienceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 5, 15, 0, 0, TimeSpan.Zero);

    // ── Domain: DxScore ───────────────────────────────────────────────────

    [Fact]
    public void DxScore_Create_WithEliteMetrics_ShouldReturnEliteLevel()
    {
        var result = DxScore.Create(
            "team-alpha", "Alpha Team", null, "weekly",
            0.5m, 5m, 2m, 5m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScoreLevel.Should().Be("Elite");
        result.Value.OverallScore.Should().BeGreaterThanOrEqualTo(80m);
    }

    [Fact]
    public void DxScore_Create_WithLowMetrics_ShouldReturnLowLevel()
    {
        var result = DxScore.Create(
            "team-legacy", "Legacy Team", null, "monthly",
            500m, 0m, 9m, 90m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScoreLevel.Should().Be("Low");
    }

    [Fact]
    public void DxScore_Create_WithInvalidPeriod_ShouldFail()
    {
        var result = DxScore.Create(
            "team-1", "Team 1", null, "daily",
            24m, 1m, 5m, 20m, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_DX_PERIOD");
    }

    [Fact]
    public void DxScore_Create_WithNegativeCycleTime_ShouldFail()
    {
        var result = DxScore.Create(
            "team-1", "Team 1", null, "weekly",
            -1m, 1m, 5m, 20m, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CYCLE_TIME");
    }

    [Fact]
    public void DxScore_Create_WithExcessiveCognitiveLoad_ShouldFail()
    {
        var result = DxScore.Create(
            "team-1", "Team 1", null, "monthly",
            24m, 1m, 15m, 20m, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_COGNITIVE_LOAD");
    }

    [Fact]
    public void DxScore_Create_WithOptionalServiceId_ShouldSucceed()
    {
        var result = DxScore.Create(
            "team-1", "Team 1", "svc-specific", "quarterly",
            48m, 2m, 4m, 30m, "Good quarter", FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-specific");
        result.Value.Notes.Should().Be("Good quarter");
    }

    // ── Domain: ProductivitySnapshot ─────────────────────────────────────

    [Fact]
    public void ProductivitySnapshot_Create_WithValidInputs_ShouldSucceed()
    {
        var start = FixedNow.AddDays(-7);
        var end = FixedNow;

        var result = ProductivitySnapshot.Create(
            "team-1", null, start, end,
            12, 8m, 1, 5, "CI", FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("team-1");
        result.Value.DeploymentCount.Should().Be(12);
    }

    [Fact]
    public void ProductivitySnapshot_Create_WhenPeriodEndBeforeStart_ShouldFail()
    {
        var start = FixedNow;
        var end = FixedNow.AddDays(-1);

        var result = ProductivitySnapshot.Create(
            "team-1", null, start, end, 0, 0m, 0, 0, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PERIOD");
    }

    // ── Handler: ComputeDeveloperExperienceScore ──────────────────────────

    [Fact]
    public async Task ComputeDeveloperExperienceScore_WhenHighPerformance_ShouldReturnEliteScore()
    {
        var repo = Substitute.For<IDxScoreRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new ComputeDeveloperExperienceScore.Handler(repo, uow, clock);
        var cmd = new ComputeDeveloperExperienceScore.Command(
            "team-devops", "DevOps Team", null, "weekly",
            0.5m, 5m, 1m, 5m, "Excellent week");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScoreLevel.Should().Be("Elite");
        result.Value.TeamId.Should().Be("team-devops");
        repo.Received(1).Add(Arg.Any<DxScore>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComputeDeveloperExperienceScore_WhenPersisted_ShouldReturnCorrectPeriod()
    {
        var repo = Substitute.For<IDxScoreRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new ComputeDeveloperExperienceScore.Handler(repo, uow, clock);
        var cmd = new ComputeDeveloperExperienceScore.Command(
            "team-q1", "Q1 Team", "svc-payments", "quarterly",
            24m, 2m, 5m, 30m, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Period.Should().Be("quarterly");
    }

    // ── Handler: GetDeveloperExperienceScore ──────────────────────────────

    [Fact]
    public async Task GetDeveloperExperienceScore_WhenScoreExists_ShouldReturnIt()
    {
        var repo = Substitute.For<IDxScoreRepository>();

        var score = DxScore.Create(
            "team-alpha", "Alpha Team", null, "monthly",
            1m, 5m, 1m, 5m, null, FixedNow);
        score.IsSuccess.Should().BeTrue();

        repo.GetByTeamAsync("team-alpha", "monthly", Arg.Any<CancellationToken>())
            .Returns(score.Value);

        var handler = new GetDeveloperExperienceScore.Handler(repo);
        var result = await handler.Handle(
            new GetDeveloperExperienceScore.Query("team-alpha", "monthly"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("team-alpha");
        result.Value.ScoreLevel.Should().Be("Elite");
    }

    [Fact]
    public async Task GetDeveloperExperienceScore_WhenNotFound_ShouldReturnError()
    {
        var repo = Substitute.For<IDxScoreRepository>();
        repo.GetByTeamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DxScore?)null);

        var handler = new GetDeveloperExperienceScore.Handler(repo);
        var result = await handler.Handle(
            new GetDeveloperExperienceScore.Query("team-unknown", "weekly"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("TEAM_NOT_FOUND_DX");
    }

    // ── Handler: ListDeveloperExperienceScores ────────────────────────────

    [Fact]
    public async Task ListDeveloperExperienceScores_ShouldReturnPagedResults()
    {
        var repo = Substitute.For<IDxScoreRepository>();
        var now = FixedNow;

        var scores = Enumerable.Range(1, 5)
            .Select(i =>
            {
                var r = DxScore.Create($"team-{i}", $"Team {i}", null, "weekly",
                    1m * i, 1m, 5m, 20m, null, now);
                return r.Value!;
            })
            .ToList();

        repo.ListAsync("weekly", null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<DxScore>)scores);

        var handler = new ListDeveloperExperienceScores.Handler(repo);
        var result = await handler.Handle(
            new ListDeveloperExperienceScores.Query("weekly", null, 1, 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(5);
        result.Value.Items.Should().HaveCount(3);
    }

    // ── Handler: RecordProductivitySnapshot ──────────────────────────────

    [Fact]
    public async Task RecordProductivitySnapshot_WhenValid_ShouldPersistAndReturn()
    {
        var repo = Substitute.For<IProductivitySnapshotRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new RecordProductivitySnapshot.Handler(repo, uow, clock);
        var cmd = new RecordProductivitySnapshot.Command(
            "team-delta", "svc-checkout",
            FixedNow.AddDays(-7), FixedNow,
            8, 6m, 1, 3, "GitLab");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("team-delta");
        result.Value.DeploymentCount.Should().Be(8);
        repo.Received(1).Add(Arg.Any<ProductivitySnapshot>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Validator tests ───────────────────────────────────────────────────

    [Fact]
    public void ComputeDeveloperExperienceScore_Validator_WhenInvalidPeriod_ShouldFail()
    {
        var validator = new ComputeDeveloperExperienceScore.Validator();
        var cmd = new ComputeDeveloperExperienceScore.Command(
            "team-1", "Team 1", null, "daily",
            24m, 1m, 5m, 20m, null);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Valid periods"));
    }

    [Fact]
    public void RecordProductivitySnapshot_Validator_WhenNegativeDeploymentCount_ShouldFail()
    {
        var validator = new RecordProductivitySnapshot.Validator();
        var cmd = new RecordProductivitySnapshot.Command(
            "team-1", null,
            FixedNow.AddDays(-7), FixedNow,
            -1, 0m, 0, 0, null);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }
}
