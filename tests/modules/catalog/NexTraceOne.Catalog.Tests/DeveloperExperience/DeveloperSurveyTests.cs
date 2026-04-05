using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperNpsSummary;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.SubmitDeveloperSurvey;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Tests.DeveloperExperience;

/// <summary>
/// Testes unitários para Developer Survey e NPS (Phase 5.2B).
/// Cobrem: DeveloperSurvey domain, SubmitDeveloperSurvey handler, GetDeveloperNpsSummary handler.
/// </summary>
public sealed class DeveloperSurveyTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 5, 16, 0, 0, TimeSpan.Zero);

    // ── Helpers ───────────────────────────────────────────────────────────

    private static DeveloperSurvey CreateSurvey(int npsScore = 9, decimal tool = 4m, decimal process = 4m, decimal platform = 4m)
    {
        var result = DeveloperSurvey.Create(
            "team-alpha", "Alpha Team", null, "respondent-1", "monthly",
            npsScore, tool, process, platform, null, FixedNow);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    // ── Domain: DeveloperSurvey.Create ────────────────────────────────────

    [Fact]
    public void DeveloperSurvey_Create_WithNpsScore9_ShouldBePromoter()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "monthly",
            9, 4m, 4m, 4m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsCategory.Should().Be("Promoter");
        result.Value.NpsScore.Should().Be(9);
    }

    [Fact]
    public void DeveloperSurvey_Create_WithNpsScore10_ShouldBePromoter()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "weekly",
            10, 5m, 5m, 5m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsCategory.Should().Be("Promoter");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithNpsScore7_ShouldBePassive()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "quarterly",
            7, 3m, 3m, 3m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsCategory.Should().Be("Passive");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithNpsScore8_ShouldBePassive()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "monthly",
            8, 3m, 3m, 3m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsCategory.Should().Be("Passive");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithNpsScore6_ShouldBeDetractor()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "monthly",
            6, 2m, 2m, 2m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsCategory.Should().Be("Detractor");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithNpsScore0_ShouldBeDetractor()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "weekly",
            0, 1m, 1m, 1m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsCategory.Should().Be("Detractor");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithInvalidNpsScore_ShouldFail()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "monthly",
            11, 3m, 3m, 3m, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_NPS_SCORE");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithNegativeNpsScore_ShouldFail()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "monthly",
            -1, 3m, 3m, 3m, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_NPS_SCORE");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithInvalidToolSatisfaction_ShouldFail()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "monthly",
            8, 6m, 3m, 3m, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_TOOL_SATISFACTION");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithInvalidPeriod_ShouldFail()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "daily",
            8, 3m, 3m, 3m, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_SURVEY_PERIOD");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithEmptyTeamId_ShouldFail()
    {
        var result = DeveloperSurvey.Create(
            "", "Team One", null, "resp-1", "monthly",
            8, 3m, 3m, 3m, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_TEAM_ID");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithCommentsTooLong_ShouldFail()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "monthly",
            8, 3m, 3m, 3m, new string('x', 2001), FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_COMMENTS");
    }

    // ── SubmitDeveloperSurvey handler ─────────────────────────────────────

    [Fact]
    public async Task SubmitDeveloperSurvey_HappyPath_ShouldPersistAndReturnPromoter()
    {
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new SubmitDeveloperSurvey.Handler(repository, unitOfWork, clock);
        var command = new SubmitDeveloperSurvey.Command(
            "team-alpha", "Alpha Team", null, "resp-1", "monthly",
            9, 4m, 4m, 4m, "Great platform!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsCategory.Should().Be("Promoter");
        result.Value.NpsScore.Should().Be(9);
        result.Value.TeamId.Should().Be("team-alpha");
        result.Value.SubmittedAt.Should().Be(FixedNow);
        await repository.Received(1).AddAsync(Arg.Any<DeveloperSurvey>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitDeveloperSurvey_WithInvalidNps_ShouldReturnFailure()
    {
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new SubmitDeveloperSurvey.Handler(repository, unitOfWork, clock);
        var command = new SubmitDeveloperSurvey.Command(
            "team-alpha", "Alpha Team", null, "resp-1", "monthly",
            15, 4m, 4m, 4m, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await repository.DidNotReceive().AddAsync(Arg.Any<DeveloperSurvey>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitDeveloperSurvey_WithDetractorScore_ShouldReturnDetractor()
    {
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new SubmitDeveloperSurvey.Handler(repository, unitOfWork, clock);
        var command = new SubmitDeveloperSurvey.Command(
            "team-beta", "Beta Team", null, "resp-2", "weekly",
            3, 2m, 2m, 2m, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsCategory.Should().Be("Detractor");
    }

    // ── GetDeveloperNpsSummary handler ────────────────────────────────────

    [Fact]
    public async Task GetDeveloperNpsSummary_WithAllPromoters_ShouldReturnNps100()
    {
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var surveys = new List<DeveloperSurvey>
        {
            CreateSurvey(npsScore: 9),
            CreateSurvey(npsScore: 10),
            CreateSurvey(npsScore: 9)
        };
        repository.ListByTeamAsync("team-alpha", null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(surveys);

        var handler = new GetDeveloperNpsSummary.Handler(repository);
        var result = await handler.Handle(new GetDeveloperNpsSummary.Query("team-alpha", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsScore.Should().Be(100m);
        result.Value.PromoterCount.Should().Be(3);
        result.Value.DetractorCount.Should().Be(0);
        result.Value.TotalResponses.Should().Be(3);
    }

    [Fact]
    public async Task GetDeveloperNpsSummary_WithAllDetractors_ShouldReturnNpsMinus100()
    {
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var surveys = new List<DeveloperSurvey>
        {
            CreateSurvey(npsScore: 0),
            CreateSurvey(npsScore: 3),
            CreateSurvey(npsScore: 6)
        };
        repository.ListByTeamAsync("team-gamma", null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(surveys);

        var handler = new GetDeveloperNpsSummary.Handler(repository);
        var result = await handler.Handle(new GetDeveloperNpsSummary.Query("team-gamma", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsScore.Should().Be(-100m);
        result.Value.DetractorCount.Should().Be(3);
        result.Value.PromoterCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDeveloperNpsSummary_WithMixedResponses_ShouldCalculateCorrectNps()
    {
        // 2 promoters (9,10), 1 passive (7), 2 detractors (4,6) → NPS = ((2-2)*100)/5 = 0
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var surveys = new List<DeveloperSurvey>
        {
            CreateSurvey(npsScore: 9),
            CreateSurvey(npsScore: 10),
            CreateSurvey(npsScore: 7),
            CreateSurvey(npsScore: 4),
            CreateSurvey(npsScore: 6)
        };
        repository.ListByTeamAsync("team-delta", "monthly", 1, 20, Arg.Any<CancellationToken>())
            .Returns(surveys);

        var handler = new GetDeveloperNpsSummary.Handler(repository);
        var result = await handler.Handle(new GetDeveloperNpsSummary.Query("team-delta", "monthly"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsScore.Should().Be(0m);
        result.Value.PromoterCount.Should().Be(2);
        result.Value.PassiveCount.Should().Be(1);
        result.Value.DetractorCount.Should().Be(2);
        result.Value.TotalResponses.Should().Be(5);
    }

    [Fact]
    public async Task GetDeveloperNpsSummary_WithNoResponses_ShouldReturnZeroSummary()
    {
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        repository.ListByTeamAsync("team-empty", null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<DeveloperSurvey>());

        var handler = new GetDeveloperNpsSummary.Handler(repository);
        var result = await handler.Handle(new GetDeveloperNpsSummary.Query("team-empty", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalResponses.Should().Be(0);
        result.Value.NpsScore.Should().Be(0m);
        result.Value.AvgToolSatisfaction.Should().Be(0m);
        result.Value.AvgProcessSatisfaction.Should().Be(0m);
        result.Value.AvgPlatformSatisfaction.Should().Be(0m);
    }

    [Fact]
    public async Task GetDeveloperNpsSummary_SatisfactionAverages_ShouldBeComputedCorrectly()
    {
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var surveys = new List<DeveloperSurvey>
        {
            CreateSurvey(npsScore: 9, tool: 4m, process: 3m, platform: 5m),
            CreateSurvey(npsScore: 8, tool: 2m, process: 5m, platform: 3m)
        };
        repository.ListByTeamAsync("team-avg", null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(surveys);

        var handler = new GetDeveloperNpsSummary.Handler(repository);
        var result = await handler.Handle(new GetDeveloperNpsSummary.Query("team-avg", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AvgToolSatisfaction.Should().Be(3.00m);
        result.Value.AvgProcessSatisfaction.Should().Be(4.00m);
        result.Value.AvgPlatformSatisfaction.Should().Be(4.00m);
    }

    [Fact]
    public async Task GetDeveloperNpsSummary_NpsScoreCalculation_Rounded1Decimal()
    {
        // 2 promoters, 0 passive, 1 detractor → NPS = (2-1)*100/3 = 33.3
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var surveys = new List<DeveloperSurvey>
        {
            CreateSurvey(npsScore: 9),
            CreateSurvey(npsScore: 10),
            CreateSurvey(npsScore: 2)
        };
        repository.ListByTeamAsync("team-nps", null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(surveys);

        var handler = new GetDeveloperNpsSummary.Handler(repository);
        var result = await handler.Handle(new GetDeveloperNpsSummary.Query("team-nps", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NpsScore.Should().Be(33.3m);
    }

    [Fact]
    public async Task GetDeveloperNpsSummary_PercentagesCalculated_Correctly()
    {
        // 4 surveys: 2 promoters, 1 passive, 1 detractor
        var repository = Substitute.For<IDeveloperSurveyRepository>();
        var surveys = new List<DeveloperSurvey>
        {
            CreateSurvey(npsScore: 9),
            CreateSurvey(npsScore: 10),
            CreateSurvey(npsScore: 7),
            CreateSurvey(npsScore: 3)
        };
        repository.ListByTeamAsync("team-pct", null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(surveys);

        var handler = new GetDeveloperNpsSummary.Handler(repository);
        var result = await handler.Handle(new GetDeveloperNpsSummary.Query("team-pct", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PromoterPercent.Should().Be(50.0m);
        result.Value.PassivePercent.Should().Be(25.0m);
        result.Value.DetractorPercent.Should().Be(25.0m);
    }

    [Fact]
    public void DeveloperSurvey_Create_WithValidServiceId_ShouldSetServiceId()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", "svc-payments", "resp-1", "monthly",
            9, 4m, 4m, 4m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ServiceId.Should().Be("svc-payments");
    }

    [Fact]
    public void DeveloperSurvey_Create_WithMaxValidSatisfaction_ShouldSucceed()
    {
        var result = DeveloperSurvey.Create(
            "team-1", "Team One", null, "resp-1", "quarterly",
            10, 5m, 5m, 5m, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ToolSatisfaction.Should().Be(5m);
        result.Value.ProcessSatisfaction.Should().Be(5m);
        result.Value.PlatformSatisfaction.Should().Be(5m);
    }

    // ── Validator tests ───────────────────────────────────────────────────

    [Fact]
    public void SubmitDeveloperSurvey_Validator_WhenNpsOutOfRange_ShouldFail()
    {
        var validator = new SubmitDeveloperSurvey.Validator();
        var cmd = new SubmitDeveloperSurvey.Command(
            "team-1", "Team 1", null, "resp-1", "monthly",
            11, 3m, 3m, 3m, null);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetDeveloperNpsSummary_Validator_WhenTeamIdEmpty_ShouldFail()
    {
        var validator = new GetDeveloperNpsSummary.Validator();
        var query = new GetDeveloperNpsSummary.Query("", null);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
