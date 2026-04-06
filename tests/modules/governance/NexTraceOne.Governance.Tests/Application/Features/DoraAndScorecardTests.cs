using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.DTOs;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Contracts.ChangeIntelligence.ServiceInterfaces;
using NexTraceOne.Governance.Application.Features.ComputeDoraMetrics;
using NexTraceOne.Governance.Application.Features.ComputeServiceScorecard;
using NexTraceOne.Governance.Application.Features.GetDoraMetricsTrend;
using NexTraceOne.Governance.Application.Features.ListServiceScorecards;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para DORA Metrics e Service Scorecard engines.
/// </summary>
public sealed class DoraAndScorecardTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly IIncidentModule _incidents = Substitute.For<IIncidentModule>();
    private readonly IChangeIntelligenceModule _changes = Substitute.For<IChangeIntelligenceModule>();
    private readonly ICatalogGraphModule _catalog = Substitute.For<ICatalogGraphModule>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public DoraAndScorecardTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _incidents.CountOpenIncidentsAsync(Arg.Any<CancellationToken>()).Returns(2);
        _incidents.CountResolvedInLastDaysAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(8);
        _incidents.GetAverageResolutionHoursAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(4.0m);
        _incidents.GetRecurrenceRateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(12.5m);
        _incidents.GetTrendSummaryAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(
            new IncidentTrendSummary(2, 8, 4.0m, 12.5m, "Stable"));
    }

    // ── ComputeDoraMetrics ────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeDoraMetrics_ValidQuery_ReturnsAllFourMetrics()
    {
        var handler = new ComputeDoraMetrics.Handler(_changes, _incidents, _clock);
        var result = await handler.Handle(new ComputeDoraMetrics.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeploymentFrequency.Should().NotBeNull();
        result.Value.LeadTimeForChanges.Should().NotBeNull();
        result.Value.ChangeFailureRate.Should().NotBeNull();
        result.Value.MeanTimeToRestore.Should().NotBeNull();
        result.Value.ComputedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task ComputeDoraMetrics_ValidRatings_OnlyKnownValues()
    {
        var handler = new ComputeDoraMetrics.Handler(_changes, _incidents, _clock);
        var result = await handler.Handle(new ComputeDoraMetrics.Query(), CancellationToken.None);

        var validRatings = new[] { "Elite", "High", "Medium", "Low" };
        validRatings.Should().Contain(result.Value.DeploymentFrequency.Rating);
        validRatings.Should().Contain(result.Value.LeadTimeForChanges.Rating);
        validRatings.Should().Contain(result.Value.ChangeFailureRate.Rating);
        validRatings.Should().Contain(result.Value.MeanTimeToRestore.Rating);
        validRatings.Should().Contain(result.Value.OverallRating);
    }

    [Fact]
    public async Task ComputeDoraMetrics_WithServiceFilter_ReturnsServiceName()
    {
        var handler = new ComputeDoraMetrics.Handler(_changes, _incidents, _clock);
        var result = await handler.Handle(new ComputeDoraMetrics.Query("my-service", null, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("my-service");
        result.Value.PeriodDays.Should().Be(30);
    }

    [Fact]
    public async Task ComputeDoraMetrics_ZeroIncidents_HighScores()
    {
        _incidents.CountOpenIncidentsAsync(Arg.Any<CancellationToken>()).Returns(0);
        _incidents.CountResolvedInLastDaysAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(0);
        _incidents.GetAverageResolutionHoursAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(0m);
        _incidents.GetRecurrenceRateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(0m);
        _incidents.GetTrendSummaryAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(
            new IncidentTrendSummary(0, 0, 0m, 0m, "Stable"));

        var handler = new ComputeDoraMetrics.Handler(_changes, _incidents, _clock);
        var result = await handler.Handle(new ComputeDoraMetrics.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeFailureRate.Value.Should().Be(0m);
    }

    [Fact]
    public void ComputeDoraMetrics_InvalidPeriodDays_FailsValidation()
    {
        var validator = new ComputeDoraMetrics.Validator();
        var result = validator.Validate(new ComputeDoraMetrics.Query(PeriodDays: 5));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ComputeDoraMetrics_ValidQuery_PassesValidation()
    {
        var validator = new ComputeDoraMetrics.Validator();
        var result = validator.Validate(new ComputeDoraMetrics.Query(PeriodDays: 30));
        result.IsValid.Should().BeTrue();
    }

    // ── GetDoraMetricsTrend ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDoraMetricsTrend_ReturnsCorrectBucketCount()
    {
        var handler = new GetDoraMetricsTrend.Handler(_incidents, _clock);
        var result = await handler.Handle(new GetDoraMetricsTrend.Query(PeriodDays: 28, BucketDays: 7), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DataPoints.Count.Should().Be(4); // 28 / 7 = 4 buckets
    }

    [Fact]
    public async Task GetDoraMetricsTrend_EachBucketHasValidValues()
    {
        var handler = new GetDoraMetricsTrend.Handler(_incidents, _clock);
        var result = await handler.Handle(new GetDoraMetricsTrend.Query(), CancellationToken.None);

        foreach (var point in result.Value.DataPoints)
        {
            point.DeploymentFrequency.Should().BeGreaterThanOrEqualTo(0m);
            point.LeadTimeHours.Should().BeGreaterThanOrEqualTo(0m);
            point.MttrHours.Should().BeGreaterThanOrEqualTo(0m);
            point.ChangeFailureRatePct.Should().BeGreaterThanOrEqualTo(0m);
            point.PeriodStart.Should().BeBefore(point.PeriodEnd);
        }
    }

    [Fact]
    public async Task GetDoraMetricsTrend_SummaryTrendIsKnownValue()
    {
        var handler = new GetDoraMetricsTrend.Handler(_incidents, _clock);
        var result = await handler.Handle(new GetDoraMetricsTrend.Query(), CancellationToken.None);

        var validTrends = new[] { "Improving", "Stable", "Degrading" };
        validTrends.Should().Contain(result.Value.Summary.MttrTrend);
        validTrends.Should().Contain(result.Value.Summary.ChangeFailureRateTrend);
        validTrends.Should().Contain(result.Value.Summary.DeploymentFrequencyTrend);
    }

    [Fact]
    public void GetDoraMetricsTrend_InvalidBucketDays_FailsValidation()
    {
        var validator = new GetDoraMetricsTrend.Validator();
        var result = validator.Validate(new GetDoraMetricsTrend.Query(BucketDays: 0));
        result.IsValid.Should().BeFalse();
    }

    // ── ComputeServiceScorecard ───────────────────────────────────────────────

    [Fact]
    public async Task ComputeServiceScorecard_ExistingService_ReturnsAllDimensions()
    {
        _catalog.ServiceAssetExistsAsync("my-service", Arg.Any<CancellationToken>()).Returns(true);
        _catalog.CountServicesByTeamAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(5);

        var handler = new ComputeServiceScorecard.Handler(_catalog, _incidents, _clock);
        var result = await handler.Handle(new ComputeServiceScorecard.Query("my-service", 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dimensions.Count.Should().Be(8);
        result.Value.ServiceName.Should().Be("my-service");
    }

    [Fact]
    public async Task ComputeServiceScorecard_ScoreWithinRange()
    {
        _catalog.ServiceAssetExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _catalog.CountServicesByTeamAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(3);

        var handler = new ComputeServiceScorecard.Handler(_catalog, _incidents, _clock);
        var result = await handler.Handle(new ComputeServiceScorecard.Query("svc-x"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FinalScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task ComputeServiceScorecard_MaturityLevelIsKnownValue()
    {
        _catalog.ServiceAssetExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _catalog.CountServicesByTeamAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(3);

        var handler = new ComputeServiceScorecard.Handler(_catalog, _incidents, _clock);
        var result = await handler.Handle(new ComputeServiceScorecard.Query("svc-y"), CancellationToken.None);

        var validLevels = new[] { "Gold", "Silver", "Bronze", "Below Standard" };
        validLevels.Should().Contain(result.Value.MaturityLevel);
    }

    [Fact]
    public async Task ComputeServiceScorecard_UnknownService_LowOwnershipScore()
    {
        _catalog.ServiceAssetExistsAsync("unknown-svc", Arg.Any<CancellationToken>()).Returns(false);
        _catalog.CountServicesByTeamAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(0);

        var handler = new ComputeServiceScorecard.Handler(_catalog, _incidents, _clock);
        var result = await handler.Handle(new ComputeServiceScorecard.Query("unknown-svc"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var ownership = result.Value.Dimensions.First(d => d.Name == "Ownership Clarity");
        ownership.Score.Should().Be(0);
    }

    [Fact]
    public void ComputeServiceScorecard_EmptyServiceName_FailsValidation()
    {
        var validator = new ComputeServiceScorecard.Validator();
        var result = validator.Validate(new ComputeServiceScorecard.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── ListServiceScorecards ─────────────────────────────────────────────────

    [Fact]
    public async Task ListServiceScorecards_WithTeamName_ReturnsListFromCatalog()
    {
        var services = new List<TeamServiceInfo>
        {
            new("svc-id-1", "payment-service", "Commerce", "High", "Direct"),
            new("svc-id-2", "notification-service", "Commerce", "Medium", "Direct"),
        };
        _catalog.ListServicesByTeamAsync("team-commerce", Arg.Any<CancellationToken>())
            .Returns(services.AsReadOnly() as IReadOnlyList<TeamServiceInfo>);

        var handler = new ListServiceScorecards.Handler(_catalog, _clock);
        var result = await handler.Handle(new ListServiceScorecards.Query("team-commerce"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(2);
    }

    [Fact]
    public async Task ListServiceScorecards_WithTeamName_OrderedByScore()
    {
        var services = new List<TeamServiceInfo>
        {
            new("id-1", "svc-alpha", "Domain1", "High", "Direct"),
            new("id-2", "svc-beta", "Domain1", "Low", "Direct"),
            new("id-3", "svc-gamma", "Domain1", "Medium", "Direct"),
        };
        _catalog.ListServicesByTeamAsync("team-x", Arg.Any<CancellationToken>())
            .Returns(services.AsReadOnly() as IReadOnlyList<TeamServiceInfo>);

        var handler = new ListServiceScorecards.Handler(_catalog, _clock);
        var result = await handler.Handle(new ListServiceScorecards.Query("team-x"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var scores = result.Value.Items.Select(s => s.FinalScore).ToList();
        scores.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ListServiceScorecards_FilterByMaturityLevel_OnlyReturnsMatching()
    {
        var services = new List<TeamServiceInfo>
        {
            new("id-1", "svc-a", "D1", "High", "Direct"),
        };
        _catalog.ListServicesByTeamAsync("team-y", Arg.Any<CancellationToken>())
            .Returns(services.AsReadOnly() as IReadOnlyList<TeamServiceInfo>);

        var handler = new ListServiceScorecards.Handler(_catalog, _clock);
        var result = await handler.Handle(new ListServiceScorecards.Query("team-y", MaturityLevel: "Gold"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        foreach (var item in result.Value.Items)
            item.MaturityLevel.Should().Be("Gold");
    }

    [Fact]
    public void ListServiceScorecards_InvalidMaturityLevel_FailsValidation()
    {
        var validator = new ListServiceScorecards.Validator();
        var result = validator.Validate(new ListServiceScorecards.Query(MaturityLevel: "InvalidLevel"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListServiceScorecards_ValidQuery_PassesValidation()
    {
        var validator = new ListServiceScorecards.Validator();
        var result = validator.Validate(new ListServiceScorecards.Query("team-z", MaturityLevel: "Gold"));
        result.IsValid.Should().BeTrue();
    }
}
